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
    public ulong IrradianceBindlessHandle { get; }
    public ulong PrefilterBindlessHandle { get; }

    private readonly Texture _irradianceRenderTarget;
    private readonly Texture _prefilterRenderTarget;

    public const int PrefilterMips = 6;

    private const int size = 128;
    private const int irradiance_size = 16;

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

        _irradianceRenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = $"_rt_Irradiance_{index}",
            Type = TextureTarget.TextureCubeMap,
            WrapMode = TextureWrapMode.ClampToEdge,
            MinFiltering = TextureMinFilter.Linear,
            InternalFormat = SizedInternalFormat.Rgb16f,
            RenderTargetParams = new RenderTargetParams
            {
                Width = irradiance_size,
                Heigth = irradiance_size,
                Attachment = FramebufferAttachment.ColorAttachment0
            }
        });

        _prefilterRenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = $"_rt_Prefilter_{index}",
            Type = TextureTarget.TextureCubeMap,
            WrapMode = TextureWrapMode.ClampToEdge,
            MaxLevels = PrefilterMips,
            InternalFormat = SizedInternalFormat.Rgb16f,
            RenderTargetParams = new RenderTargetParams
            {
                Width = size,
                Heigth = size,
                Attachment = FramebufferAttachment.ColorAttachment0
            }
        });

        IrradianceBindlessHandle = GL.ARB.GetTextureHandleARB(_irradianceRenderTarget.Handle);
        PrefilterBindlessHandle = GL.ARB.GetTextureHandleARB(_prefilterRenderTarget.Handle);

        GL.ARB.MakeTextureHandleResidentARB(PrefilterBindlessHandle);
        GL.ARB.MakeTextureHandleResidentARB(IrradianceBindlessHandle);
    }

    public void Render(Sky? sky)
    {
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        var envMap = RenderCubemap(sky);
        RenderIrradience(envMap);

        if (ConVarStorage.Get<bool>("mat_ibl_prefilter"))
            RenderPrefilter(envMap);

        envMap.Unload();
    }

    private Texture RenderCubemap(Sky? sky)
    {
        var cubemapBuffer = new FrameBuffer();
        cubemapBuffer.Bind();

        RenderBuffer.Create(InternalFormat.DepthComponent, FramebufferAttachment.DepthAttachment, size, size);

        var cubemapRenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = $"_rt_EnvironmentMap_{_index}",
            Type = TextureTarget.TextureCubeMap,
            WrapMode = TextureWrapMode.ClampToEdge,
            InternalFormat = SizedInternalFormat.Rgb16f,
            RenderTargetParams = new RenderTargetParams
            {
                Width = size,
                Heigth = size,
                Attachment = FramebufferAttachment.ColorAttachment0
            }
        });
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

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

            GL.NamedFramebufferTextureLayer(cubemapBuffer.Handle,
                FramebufferAttachment.ColorAttachment0,
                cubemapRenderTarget.Handle,
                level: 0,
                layer: (int)i); // faceIndex = 0..5

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapViews[i].target, _cubemapViews[i].up);
            sky?.Draw();

            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Position, Position + _cubemapViews[i].target, _cubemapViews[i].up);
            Engine.MeshManager.Draw(false, frustum: Engine.MainViewport.GetFrustum());

            cubemapBuffer.Unbind();
        }

        GL.GenerateTextureMipmap(cubemapRenderTarget.Handle);

        cubemapBuffer.Unload();

        return cubemapRenderTarget;
    }

    private void RenderIrradience(Texture envMap)
    {
        var irradianceShader = new Irradiance(envMap);
        envMap.References++; // todo: this should be done automatically

        var irradianceBuffer = new FrameBuffer();
        irradianceBuffer.Bind();

        var name = $"ibl_{_index}_irradiance_framebuffer";
        GL.ObjectLabel(ObjectIdentifier.Framebuffer, (uint)irradianceBuffer.Handle, name.Length, name);

        RenderBuffer.Create(InternalFormat.DepthComponent, FramebufferAttachment.DepthAttachment, irradiance_size, irradiance_size);

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        irradianceBuffer.Check();
        irradianceBuffer.Unbind();

        GL.Viewport(0, 0, irradiance_size, irradiance_size);

        irradianceBuffer.Bind();
        CommonShapes.CubeVertexArray?.Bind();

        Engine.MainViewport.ProjectionMatrixOverride = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90f), 1.0f, 0.1f, 2f);

        for (uint i = 0; i < 6; i++)
        {
            GL.NamedFramebufferTextureLayer(irradianceBuffer.Handle,
                FramebufferAttachment.ColorAttachment0,
                _irradianceRenderTarget.Handle,
                level: 0,
                layer: (int)i); // faceIndex = 0..5

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            irradianceShader.Bind();

            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapUsageViews[i].target, _cubemapUsageViews[i].up);

            GL.DrawArrays(PrimitiveType.Triangles, 0, CommonShapes.Cube.Length);
            PerformanceMeasurment.Increment("DrawCalls");

            irradianceShader.Unbind();
        }

        CommonShapes.CubeVertexArray?.Unbind();
        irradianceBuffer.Unbind();

        irradianceBuffer.Unload();
        irradianceShader.Unload();
    }

    private void RenderPrefilter(Texture envMap)
    {
        var prefilterShader = new Prefiltering(envMap);
        envMap.References++; // todo: this should be done automatically

        var prefilterBuffer = new FrameBuffer();
        prefilterBuffer.Bind();

        var name = $"ibl_{_index}_prefilter_framebuffer";
        GL.ObjectLabel(ObjectIdentifier.Framebuffer, (uint)prefilterBuffer.Handle, name.Length, name);

        var prefilterRenderbuffer = new RenderBuffer(InternalFormat.DepthComponent, FramebufferAttachment.DepthAttachment, size, size);

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

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
                GL.NamedFramebufferTextureLayer(prefilterBuffer.Handle,
                    FramebufferAttachment.ColorAttachment0,
                    _prefilterRenderTarget.Handle,
                    level: mip,
                    layer: (int)face
                );

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                prefilterShader.Bind();
                prefilterShader.SetFloat("roughness", roughness);
                prefilterShader.SetInt("mip", mip);

                Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapUsageViews[face].target, _cubemapUsageViews[face].up);

                GL.DrawArrays(PrimitiveType.Triangles, 0, CommonShapes.Cube.Length);
                PerformanceMeasurment.Increment("DrawCalls");

                prefilterShader.Unbind();
            }
        }

        CommonShapes.CubeVertexArray?.Unbind();
        prefilterBuffer.Unbind();

        prefilterBuffer.Unload();
        prefilterShader.Unload();
    }

    public void Unload()
    {
        GL.ARB.MakeTextureHandleNonResidentARB(IrradianceBindlessHandle);
        GL.ARB.MakeTextureHandleNonResidentARB(PrefilterBindlessHandle);

        _irradianceRenderTarget.Unload();
        _prefilterRenderTarget.Unload();
    }
}

public class ImageBasedLighting
{
    public List<LightProbe> Probes { get; } = new();

    public readonly ShaderStorageBuffer<LightProbes> LightProbesSsbo = new("lightProbesSSBO", new LightProbes());
    public const int max_probes = 512;

    public LightProbe? AddProbe(Vector3 position)
    {
        if (Probes.Count >= max_probes)
            return null;

        var probe = new LightProbe(Probes.Count)
        {
            Position = position
        };
        Probes.Add(probe);

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
        var gpuProbes = ArrayPool<Jellyfish.Render.Shaders.Structs.LightProbe>.Shared.Rent(max_probes);

        if (Probes.Count == 0)
        {
            LightProbesSsbo.UpdateData(new LightProbes
            {
                Probes = gpuProbes,
                ProbeCount = 0
            });

            ArrayPool<Jellyfish.Render.Shaders.Structs.LightProbe>.Shared.Return(gpuProbes);
            return;
        }

        var iblState = ConVarStorage.Get<bool>("mat_ibl_enabled");
        var sslrState = ConVarStorage.Get<bool>("mat_sslr_enabled");

        ConVarStorage.Set("mat_ibl_enabled", false);
        ConVarStorage.Set("mat_sslr_enabled", false);

        foreach (var lightProbe in Probes)
        {
            lightProbe.Render(sky);
        }

        ConVarStorage.Set("mat_ibl_enabled", iblState);
        ConVarStorage.Set("mat_sslr_enabled", sslrState);

        Engine.MainViewport.ViewMatrixOverride = null;
        Engine.MainViewport.ProjectionMatrixOverride = null;

        for (var i = 0; i < Probes.Count; i++)
        {
            gpuProbes[i].Position = new Vector4(Probes[i].Position);
            gpuProbes[i].IrradianceTexture = Probes[i].IrradianceBindlessHandle;
            gpuProbes[i].PrefilterTexture = Probes[i].PrefilterBindlessHandle;
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
    }

    public void Unload()
    {
        Reset();
    }

    public void GenerateProbeGrid()
    {
        var xStep = (int)(Engine.MeshManager.SceneBoundingBox.Size.X / 6);
        var yStep = (int)(Engine.MeshManager.SceneBoundingBox.Size.Y / 4);
        var zStep = (int)(Engine.MeshManager.SceneBoundingBox.Size.Z / 6);

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