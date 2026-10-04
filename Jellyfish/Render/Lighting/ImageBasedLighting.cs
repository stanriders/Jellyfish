using System;
using Jellyfish.Console;
using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders.IBL;
using Jellyfish.Render.Shaders.Structs;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Render.Lighting;

public class IblEnabled() : ConVar<bool>("mat_ibl_enabled", true);
public class IblPrefilter() : ConVar<bool>("mat_ibl_prefilter", true);

public class LightProbe
{
    private readonly int _index;
    public Vector3 Position { get; set; }
    public float Radius { get; set; }
    public ulong PrefilterBindlessHandle { get; }
    public Vector4[] IrradianceHarmonics { get; } = new Vector4[9];

    private readonly Texture _prefilterRenderTarget;

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

        PrefilterBindlessHandle = GL.ARB.GetTextureHandleARB(_prefilterRenderTarget.Handle);

        GL.ARB.MakeTextureHandleResidentARB(PrefilterBindlessHandle);
    }

    public void Render(Sky? sky)
    {
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        var envMap = RenderCubemap(sky);
        ComputeIrradianceHarmonics(envMap);

        if (ConVarStorage.Get<bool>("mat_ibl_prefilter"))
            RenderPrefilter(envMap);

        envMap.Unload();
    }

    private Texture RenderCubemap(Sky? sky)
    {
        using var cubemapBuffer = new FrameBuffer();
        cubemapBuffer.Bind();

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
        cubemapBuffer.Unbind();

        Engine.MainViewport.ProjectionMatrixOverride = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90f), 1.0f, 0.1f, 2000f);

        for (uint i = 0; i < 6; i++)
        {
            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Position, Position + _cubemapViews[i].target, _cubemapViews[i].up);
            Engine.Renderer.PreFrame();

            GL.Viewport(0, 0, size, size);

            cubemapBuffer.Bind();

            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

            cubemapBuffer.AttachTexture(cubemapRenderTarget.Handle, layer: (int)i);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapViews[i].target, _cubemapViews[i].up);
            sky?.Draw();

            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Position, Position + _cubemapViews[i].target, _cubemapViews[i].up);
            Engine.MeshManager.Draw(false, frustum: Engine.MainViewport.GetFrustum());

            cubemapBuffer.Unbind();
        }

        GL.GenerateTextureMipmap(cubemapRenderTarget.Handle);

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

    private void RenderPrefilter(Texture envMap)
    {
        var prefilterShader = new Prefiltering(envMap);
        envMap.References++; // todo: this should be done automatically

        using var prefilterBuffer = new FrameBuffer();
        prefilterBuffer.Bind();

        var name = $"ibl_{_index}_prefilter_framebuffer";
        GL.ObjectLabel(ObjectIdentifier.Framebuffer, prefilterBuffer.Handle, name.Length, name);

        var prefilterRenderbuffer = new RenderBuffer(InternalFormat.DepthComponent, size, size);

        prefilterBuffer.AttachRenderbuffer(prefilterRenderbuffer.Handle, FramebufferAttachment.DepthAttachment);
        prefilterBuffer.DrawInto(ColorBuffer.ColorAttachment0);
        prefilterBuffer.Check();
        prefilterBuffer.Unbind();

        GL.Viewport(0, 0, size, size);

        prefilterBuffer.Bind();
        CommonShapes.CubeVertexArray?.Bind();

        Engine.MainViewport.ProjectionMatrixOverride = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90f), 1.0f, 0.1f, 2f);

        var maxMipLevels = _prefilterRenderTarget.Levels;
        for (var mip = 0; mip < maxMipLevels; mip++)
        {
            var mipWidth = size >> mip;
            var mipHeight = size >> mip;

            GL.Viewport(0, 0, mipWidth, mipHeight);
            prefilterRenderbuffer.Bind();
            prefilterRenderbuffer.UpdateSize(mipWidth, mipHeight);

            var roughness = mip / (float)(maxMipLevels - 1);

            for (uint face = 0; face < 6; face++)
            {
                prefilterBuffer.AttachTexture(_prefilterRenderTarget.Handle, level: mip, layer: (int)face);

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                prefilterShader.Bind();
                prefilterShader.SetFloat("roughness", roughness);
                prefilterShader.SetInt("mip", mip);
                prefilterShader.SetFloat("envMapResolution", size);

                Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapUsageViews[face].target, _cubemapUsageViews[face].up);

                GL.DrawArrays(PrimitiveType.Triangles, 0, CommonShapes.Cube.Length);
                PerformanceMeasurement.Increment("DrawCalls");

                prefilterShader.Unbind();
            }
        }

        CommonShapes.CubeVertexArray?.Unbind();
        prefilterBuffer.Unbind();

        prefilterShader.Unload();
    }

    public void Unload()
    {
        GL.ARB.MakeTextureHandleNonResidentARB(PrefilterBindlessHandle);

        _prefilterRenderTarget.Unload();
    }
}

public class ImageBasedLighting
{
    public List<LightProbe> Probes { get; } = new();
    private int _probesCount;

    public readonly ShaderStorageBuffer<LightProbes> LightProbesSsbo = new("lightProbesSSBO", new LightProbes());
    public const int max_probes = 512;

    public LightProbe? AddProbe(Vector3 position)
    {
        if (Probes.Count >= max_probes)
            return null;

        var probe = new LightProbe(_probesCount)
        {
            Position = position
        };
        Probes.Add(probe);
        _probesCount++;

        return probe;
    }

    public void RemoveProbe(LightProbe probe)
    {
        probe.Unload();
        Probes.Remove(probe);
    }

    public void Frame(Sky? sky)
    {
        //Render(sky);
    }

    public void Render(Sky? sky)
    {
        if (Probes.Count == 0)
        {
            UpdateProbeBuffer();
            return;
        }

        var iblState = ConVarStorage.Get<bool>("mat_ibl_enabled");
        var sslrState = ConVarStorage.Get<bool>("mat_sslr_enabled");

        ConVarStorage.Set("mat_ibl_enabled", false);
        ConVarStorage.Set("mat_sslr_enabled", false);

        UpdateProbeRadii();

        foreach (var lightProbe in Probes)
        {
            lightProbe.Render(sky);
        }

        ConVarStorage.Set("mat_ibl_enabled", iblState);
        ConVarStorage.Set("mat_sslr_enabled", sslrState);
        
        Engine.MainViewport.ViewMatrixOverride = null;
        Engine.MainViewport.ProjectionMatrixOverride = null;

        UpdateProbeBuffer();

        // second pass to simulate one bounce of lighting
        foreach (var lightProbe in Probes)
        {
            lightProbe.Render(sky);
        }

        // SH lives in the buffer (unlike textures that got updated in place), so upload the bounced result too
        UpdateProbeBuffer();

        Engine.MainViewport.ViewMatrixOverride = null;
        Engine.MainViewport.ProjectionMatrixOverride = null;
    }

    private void UpdateProbeRadii()
    {
        var distances = new List<float>(Probes.Count);

        foreach (var probe in Probes)
        {
            distances.Clear();
            foreach (var other in Probes)
            {
                if (other != probe)
                    distances.Add((other.Position - probe.Position).Length);
            }

            if (distances.Count == 0)
            {
                probe.Radius = 100_000f; // lone probe should light everything
                continue;
            }

            distances.Sort();

            var neighbourDistance = distances[Math.Min(4, distances.Count) - 1];
            probe.Radius = Math.Max(neighbourDistance, 0.1f);
        }
    }

    private void UpdateProbeBuffer()
    {
        var gpuProbes = ArrayPool<Jellyfish.Render.Shaders.Structs.LightProbe>.Shared.Rent(max_probes);

        for (var i = 0; i < Probes.Count; i++)
        {
            var probe = Probes[i];

            gpuProbes[i].Position = new Vector4(probe.Position, probe.Radius);
            gpuProbes[i].PrefilterTexture = probe.PrefilterBindlessHandle;
            gpuProbes[i].Sh0 = probe.IrradianceHarmonics[0];
            gpuProbes[i].Sh1 = probe.IrradianceHarmonics[1];
            gpuProbes[i].Sh2 = probe.IrradianceHarmonics[2];
            gpuProbes[i].Sh3 = probe.IrradianceHarmonics[3];
            gpuProbes[i].Sh4 = probe.IrradianceHarmonics[4];
            gpuProbes[i].Sh5 = probe.IrradianceHarmonics[5];
            gpuProbes[i].Sh6 = probe.IrradianceHarmonics[6];
            gpuProbes[i].Sh7 = probe.IrradianceHarmonics[7];
            gpuProbes[i].Sh8 = probe.IrradianceHarmonics[8];
        }

        LightProbesSsbo.UpdateData(new LightProbes
        {
            Probes = gpuProbes,
            ProbeCount = Probes.Count
        });

        ArrayPool<Jellyfish.Render.Shaders.Structs.LightProbe>.Shared.Return(gpuProbes);
    }

    public void Reset()
    {
        foreach (var lightProbe in Probes)
        {
            lightProbe.Unload();
        }

        Probes.Clear();
        _probesCount = 0;
    }

    public void Unload()
    {
        Reset();
    }

    public void GenerateProbeGrid()
    {
        // don't inf loop on empty maps
        if (Engine.MeshManager.SceneBoundingBox.Radius < 6)
            return;

        var xStep = (int)Math.Max(1, Engine.MeshManager.SceneBoundingBox.Size.X / 6);
        var yStep = (int)Math.Max(1, Engine.MeshManager.SceneBoundingBox.Size.Y / 4);
        var zStep = (int)Math.Max(1, Engine.MeshManager.SceneBoundingBox.Size.Z / 6);

        for (var xOffset = (int)Engine.MeshManager.SceneBoundingBox.Min.X + xStep;
             xOffset < (int)Engine.MeshManager.SceneBoundingBox.Max.X;
             xOffset += xStep)
        {
            for (var yOffset = (int)Engine.MeshManager.SceneBoundingBox.Min.Y + yStep;
                 yOffset < (int)Engine.MeshManager.SceneBoundingBox.Max.Y;
                 yOffset += yStep)
            {
                for (var zOffset = (int)Engine.MeshManager.SceneBoundingBox.Min.Z + zStep;
                     zOffset < (int)Engine.MeshManager.SceneBoundingBox.Max.Z;
                     zOffset += zStep)
                {
                    var offset = new Vector3(xOffset, yOffset, zOffset);
                    if (!Probes.Any(x => x.Position == offset))
                        AddProbe(offset);
                }
            }
        }
    }
}