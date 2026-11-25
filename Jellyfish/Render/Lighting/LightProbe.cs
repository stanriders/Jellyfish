using System;
using Jellyfish.Console;
using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders.IBL;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Lighting;

public class LightProbe
{
    private readonly int _index;
    public Vector3 Position { get; set; }
    public float Radius { get; set; }
    public ulong PrefilterBindlessHandle { get; }
    public Vector4[] IrradianceHarmonics { get; } = new Vector4[9];

    private readonly TextureRef _prefilterRenderTarget;

    public const int PrefilterMips = 6;

    private const int size = 128;

    private readonly (Vector3 target, Vector3 up)[] _cubemapViews =
    [
        (new Vector3( 1,  0,  0), new Vector3(0, -1,  0)), // +X
        (new Vector3(-1,  0,  0), new Vector3(0, -1,  0)), // -X
        (new Vector3( 0,  1,  0), new Vector3(0,  0,  1)), // +Y
        (new Vector3( 0, -1,  0), new Vector3(0,  0, -1)), // -Y
        (new Vector3( 0,  0,  1), new Vector3(0, -1,  0)), // +Z
        (new Vector3( 0,  0, -1), new Vector3(0, -1,  0)) // -Z
    ];

    // TODO: figure out why the hell can't we use one view array
    private readonly (Vector3 target, Vector3 up)[] _cubemapUsageViews =
    [
        (new Vector3(-1,  0,  0), new Vector3(0, -1,  0)), // -X
        (new Vector3( 0,  1,  0), new Vector3(0,  0,  1)), // +Y
        (new Vector3( 0, -1,  0), new Vector3(0,  0, -1)), // -Y
        (new Vector3( 0,  0,  1), new Vector3(0, -1,  0)), // +Z
        (new Vector3( 0,  0, -1), new Vector3(0, -1,  0)), // -Z
        (new Vector3( 1,  0,  0), new Vector3(0, -1,  0)), // +X
    ];

    public LightProbe(int index)
    {
        _index = index;

        _prefilterRenderTarget = Engine.TextureManager.CreateTexture(new RenderTargetParams
        {
            Width = size,
            Heigth = size,
            TextureParams = new TextureParams
            {
                Name = $"_rt_Prefilter_{index}",
                Type = TextureTarget.TextureCubeMap,
                WrapMode = TextureWrapMode.ClampToEdge,
                MaxLevels = PrefilterMips,
                InternalFormat = SizedInternalFormat.Rgb16f
            }
        });

        PrefilterBindlessHandle = GL.ARB.GetTextureHandleARB(_prefilterRenderTarget.Texture.Handle);

        GL.ARB.MakeTextureHandleResidentARB(PrefilterBindlessHandle);
    }

    public void Render(Sky? sky)
    {
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        var envMap = RenderCubemap(sky);
        ComputeIrradianceHarmonics(envMap.Texture);

        if (ConVarStorage.Get<bool>("mat_ibl_prefilter"))
            RenderPrefilter(envMap);

        envMap.Dispose();
    }

    private TextureRef RenderCubemap(Sky? sky)
    {
        using var cubemapBuffer = new FrameBuffer();
        using var renderBuffer = new RenderBuffer(InternalFormat.DepthComponent, size, size);

        var cubemapRenderTarget = Engine.TextureManager.CreateTexture(new RenderTargetParams
        {
            Width = size,
            Heigth = size,
            TextureParams = new TextureParams
            {
                Name = $"_rt_EnvironmentMap_{_index}",
                Type = TextureTarget.TextureCubeMap,
                WrapMode = TextureWrapMode.ClampToEdge,
                MaxLevels = -1,
                InternalFormat = SizedInternalFormat.Rgb16f,
            }
        });

        cubemapBuffer.AttachRenderbuffer(renderBuffer.Handle, FramebufferAttachment.DepthAttachment);
        cubemapBuffer.DrawInto(ColorBuffer.ColorAttachment0);
        cubemapBuffer.Check();

        Engine.MainViewport.ProjectionMatrixOverride = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90f), 1.0f, 0.1f, 2000f);
        Engine.MainViewport.SizeOverride = new Vector2i(size);

        for (uint i = 0; i < 6; i++)
        {
            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Position, Position + _cubemapViews[i].target, _cubemapViews[i].up);
            Engine.Renderer.PreFrame();

            GL.Viewport(0, 0, size, size);

            cubemapBuffer.Bind();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            cubemapBuffer.AttachTexture(cubemapRenderTarget.Texture.Handle, layer: (int)i);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapViews[i].target, _cubemapViews[i].up);
            sky?.Draw();

            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Position, Position + _cubemapViews[i].target, _cubemapViews[i].up);
            Engine.MeshManager.DrawOpaque(Engine.MainViewport.GetFrustum(), false);

            cubemapBuffer.Unbind();
        }

        GL.GenerateTextureMipmap(cubemapRenderTarget.Texture.Handle);

        return cubemapRenderTarget;
    }

    private void ComputeIrradianceHarmonics(Texture envMap)
    {
        // L2 SH only keeps very low frequencies, so a small mip gives the same result as the full-res faces
        const int level = 3;
        const int faceSize = size >> level;
        var pixels = new float[faceSize * faceSize * 6 * 4];
        GL.GetTextureImage(envMap.Handle, level, PixelFormat.Rgba, PixelType.Float, pixels.Length * sizeof(float), pixels);

        var sh = new Vector3[9];
        var totalWeight = 0f;

        for (var face = 0; face < 6; face++)
        for (var y = 0; y < faceSize; y++)
        for (var x = 0; x < faceSize; x++)
        {
            var s = 2f * (x + 0.5f) / faceSize - 1f;
            var t = 2f * (y + 0.5f) / faceSize - 1f;

            // OpenGL cubemap face conventions, same as what texture() uses when sampling
            var dir = face switch
            {
                0 => new Vector3(1, -t, -s),  // +X
                1 => new Vector3(-1, -t, s),  // -X
                2 => new Vector3(s, 1, t),    // +Y
                3 => new Vector3(s, -1, -t),  // -Y
                4 => new Vector3(s, -t, 1),   // +Z
                _ => new Vector3(-s, -t, -1), // -Z
            };
            dir.Normalize();

            // solid angle covered by this texel
            var tmp = 1f + s * s + t * t;
            var weight = 4f / (faceSize * faceSize * tmp * MathF.Sqrt(tmp));
            totalWeight += weight;

            var i = ((face * faceSize + y) * faceSize + x) * 4;
            var color = new Vector3(pixels[i], pixels[i + 1], pixels[i + 2]) * weight;

            sh[0] += color * 0.282095f;
            sh[1] += color * 0.488603f * dir.Y;
            sh[2] += color * 0.488603f * dir.Z;
            sh[3] += color * 0.488603f * dir.X;
            sh[4] += color * 1.092548f * dir.X * dir.Y;
            sh[5] += color * 1.092548f * dir.Y * dir.Z;
            sh[6] += color * 0.315392f * (3f * dir.Z * dir.Z - 1f);
            sh[7] += color * 1.092548f * dir.X * dir.Z;
            sh[8] += color * 0.546274f * (dir.X * dir.X - dir.Y * dir.Y);
        }

        // normalize to the full sphere and apply cosine lobe convolution per band (pi, 2pi/3, pi/4)
        // divided by pi, to match the old irradiance map which stored irradiance / pi
        var normalization = 4f * MathF.PI / totalWeight;
        ReadOnlySpan<float> bandScale = [1f, 2f / 3f, 2f / 3f, 2f / 3f, 0.25f, 0.25f, 0.25f, 0.25f, 0.25f];

        for (var k = 0; k < 9; k++)
            IrradianceHarmonics[k] = new Vector4(sh[k] * normalization * bandScale[k], 0f);
    }

    private void RenderPrefilter(TextureRef envMap)
    {
        var prefilterShader = (Prefiltering)Engine.ShaderManager.GetShader("Prefiltering")!;

        using var prefilterBuffer = new FrameBuffer($"ibl_{_index}_prefilter_framebuffer");
        var prefilterRenderbuffer = new RenderBuffer(InternalFormat.DepthComponent, size, size);

        prefilterBuffer.AttachRenderbuffer(prefilterRenderbuffer.Handle, FramebufferAttachment.DepthAttachment);
        prefilterBuffer.DrawInto(ColorBuffer.ColorAttachment0);
        prefilterBuffer.Check();

        GL.Viewport(0, 0, size, size);

        prefilterBuffer.Bind();
        CommonShapes.CubeVertexArray?.Bind();

        Engine.MainViewport.ProjectionMatrixOverride = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90f), 1.0f, 0.1f, 2f);
        Engine.MainViewport.SizeOverride = new Vector2i(size);

        var maxMipLevels = _prefilterRenderTarget.Texture.Levels;

        prefilterShader.SetEnvMap(envMap);
        prefilterShader.Bind();
        for (var mip = 0; mip < maxMipLevels; mip++)
        {
            var mipWidth = size >> mip;
            var mipHeight = size >> mip;

            GL.Viewport(0, 0, mipWidth, mipHeight);
            prefilterRenderbuffer.UpdateSize(mipWidth, mipHeight);

            var roughness = mip / (float)(maxMipLevels - 1);

            for (uint face = 0; face < 6; face++)
            {
                prefilterBuffer.AttachTexture(_prefilterRenderTarget.Texture.Handle, level: mip, layer: (int)face);

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                prefilterShader.SetFloat("roughness", roughness);
                prefilterShader.SetInt("mip", mip);
                prefilterShader.SetFloat("envMapResolution", size);

                Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapUsageViews[face].target, _cubemapUsageViews[face].up);

                GL.DrawArrays(PrimitiveType.Triangles, 0, CommonShapes.Cube.Length);
                PerformanceMeasurement.Increment("DrawCalls");
            }
        }
        prefilterShader.Unbind();

        CommonShapes.CubeVertexArray?.Unbind();
        prefilterBuffer.Unbind();
    }

    public void Unload()
    {
        GL.ARB.MakeTextureHandleNonResidentARB(PrefilterBindlessHandle);

        _prefilterRenderTarget.Dispose();
    }
}