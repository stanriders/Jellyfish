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
using System.Diagnostics;

namespace Jellyfish.Render.Lighting;

public class IblEnabled() : ConVar<bool>("mat_ibl_enabled", true);
public class IblPrefilter() : ConVar<bool>("mat_ibl_prefilter", false);
public class IblRenderWorld() : ConVar<bool>("mat_ibl_render_world", true);

public class LightProbe
{
    public Vector3 Position { get; set; }
    public ulong IrradianceBindlessHandle { get; }
    public ulong PrefilterBindlessHandle { get; }

    private readonly FrameBuffer _cubemapBuffer;
    private readonly Texture _cubemapRenderTarget;

    private readonly FrameBuffer _irradianceBuffer;
    private readonly Texture _irradianceRenderTarget;
    private readonly Irradiance _irradianceShader;

    private readonly FrameBuffer _prefilterBuffer;
    private readonly RenderBuffer _prefilterRenderbuffer;
    private readonly Texture _prefilterRenderTarget;
    private readonly Prefiltering _prefilterShader;

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
        #region cubemap
        _cubemapBuffer = new FrameBuffer();
        _cubemapBuffer.Bind();

        RenderBuffer.Create(InternalFormat.DepthComponent, FramebufferAttachment.DepthAttachment, size, size);

        _cubemapRenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = $"_rt_EnvironmentMap_{index}",
            Type = TextureTarget.TextureCubeMap,
            WrapMode = TextureWrapMode.ClampToEdge,
            RenderTargetParams = new RenderTargetParams
            {
                Width = size,
                Heigth = size,
                InternalFormat = SizedInternalFormat.Rgb16f,
                Attachment = FramebufferAttachment.ColorAttachment0
            }
        });
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        _cubemapBuffer.Check();
        _cubemapBuffer.Unbind();
        #endregion

        #region irradiance
        _irradianceShader = new Irradiance(_cubemapRenderTarget);
        _irradianceBuffer = new FrameBuffer();
        _irradianceBuffer.Bind();
        RenderBuffer.Create(InternalFormat.DepthComponent, FramebufferAttachment.DepthAttachment, irradiance_size, irradiance_size);

        _irradianceRenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = $"_rt_Irradiance_{index}",
            Type = TextureTarget.TextureCubeMap,
            WrapMode = TextureWrapMode.ClampToEdge,
            MinFiltering = TextureMinFilter.Linear,
            RenderTargetParams = new RenderTargetParams
            {
                Width = irradiance_size,
                Heigth = irradiance_size,
                InternalFormat = SizedInternalFormat.Rgb16f,
                Attachment = FramebufferAttachment.ColorAttachment0
            }
        });

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        _irradianceBuffer.Check();
        _irradianceBuffer.Unbind();

        IrradianceBindlessHandle = GL.ARB.GetTextureHandleARB(_irradianceRenderTarget.Handle);
        #endregion

        #region prefilter

        _prefilterShader = new Prefiltering(_cubemapRenderTarget);
        _prefilterBuffer = new FrameBuffer();
        _prefilterBuffer.Bind();

        _prefilterRenderbuffer = new RenderBuffer(InternalFormat.DepthComponent, FramebufferAttachment.DepthAttachment, size, size);

        _prefilterRenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = $"_rt_Prefilter_{index}",
            Type = TextureTarget.TextureCubeMap,
            WrapMode = TextureWrapMode.ClampToEdge,
            MaxLevels = null,
            RenderTargetParams = new RenderTargetParams
            {
                Width = size,
                Heigth = size,
                InternalFormat = SizedInternalFormat.Rgb16f,
                Attachment = FramebufferAttachment.ColorAttachment0
            }
        });

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        _prefilterBuffer.Check();
        _prefilterBuffer.Unbind();

        PrefilterBindlessHandle = GL.ARB.GetTextureHandleARB(_prefilterRenderTarget.Handle);
        #endregion

        GL.ARB.MakeTextureHandleResidentARB(PrefilterBindlessHandle);
        GL.ARB.MakeTextureHandleResidentARB(IrradianceBindlessHandle);
    }

    public void RenderCubemap(Sky? sky)
    {
        var renderWorld = ConVarStorage.Get<bool>("mat_ibl_render_world");
        GL.Viewport(0, 0, size, size);

        _cubemapBuffer.Bind();

        Engine.MainViewport.ProjectionMatrixOverride = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90f), 1.0f, 0.1f, renderWorld ? 2000f : 2.0f);

        for (uint i = 0; i < 6; i++)
        {
            GL.NamedFramebufferTextureLayer(_cubemapBuffer.Handle,
                FramebufferAttachment.ColorAttachment0,
                _cubemapRenderTarget.Handle,
                level: 0,
                layer: (int)i); // faceIndex = 0..5

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapViews[i].target, _cubemapViews[i].up);

            sky?.Draw();

            if (renderWorld)
            {
                // todo: very, VERY expensive
                Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Position, Position + _cubemapViews[i].target, _cubemapViews[i].up);
                Engine.MeshManager.Draw(false, frustum: Engine.MainViewport.GetFrustum());
            }
        }

        GL.GenerateTextureMipmap(_cubemapBuffer.Handle);

        _cubemapBuffer.Unbind();
    }

    public void RenderIrradience()
    {
        GL.Viewport(0, 0, irradiance_size, irradiance_size);

        _irradianceBuffer.Bind();
        CommonShapes.CubeVertexArray?.Bind();

        Engine.MainViewport.ProjectionMatrixOverride = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90f), 1.0f, 0.1f, 2f);

        for (uint i = 0; i < 6; i++)
        {
            GL.NamedFramebufferTextureLayer(_irradianceBuffer.Handle,
                FramebufferAttachment.ColorAttachment0,
                _irradianceRenderTarget.Handle,
                level: 0,
                layer: (int)i); // faceIndex = 0..5

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            _irradianceShader.Bind();

            Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapUsageViews[i].target, _cubemapUsageViews[i].up);

            GL.DrawArrays(PrimitiveType.Triangles, 0, CommonShapes.Cube.Length);
            PerformanceMeasurment.Increment("DrawCalls");

            _irradianceShader.Unbind();
        }

        CommonShapes.CubeVertexArray?.Unbind();
        _irradianceBuffer.Unbind();
    }

    public void RenderPrefilter()
    {
        GL.Viewport(0, 0, size, size);

        _prefilterBuffer.Bind();
        CommonShapes.CubeVertexArray?.Bind();

        Engine.MainViewport.ProjectionMatrixOverride = Matrix4.CreatePerspectiveFieldOfView(float.DegreesToRadians(90f), 1.0f, 0.1f, 2f);

        var maxMipLevels = _prefilterRenderTarget.Levels;
        for (var mip = 0; mip < maxMipLevels; mip++)
        {
            var mipWidth = size >> mip;
            var mipHeight = size >> mip;

            GL.Viewport(0, 0, mipWidth, mipHeight);
            _prefilterRenderbuffer.Bind();
            _prefilterRenderbuffer.UpdateSize(mipWidth, mipHeight);

            var roughness = mip / (float)(maxMipLevels - 1);

            for (uint face = 0; face < 6; face++)
            {
                GL.NamedFramebufferTextureLayer(_prefilterBuffer.Handle,
                    FramebufferAttachment.ColorAttachment0,
                    _prefilterRenderTarget.Handle,
                    level: mip,
                    layer: (int)face
                );

                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

                _prefilterShader.Bind();
                _prefilterShader.SetFloat("roughness", roughness);
                _prefilterShader.SetInt("mip", mip);

                Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Vector3.Zero, _cubemapUsageViews[face].target, _cubemapUsageViews[face].up);

                GL.DrawArrays(PrimitiveType.Triangles, 0, CommonShapes.Cube.Length);
                PerformanceMeasurment.Increment("DrawCalls");

                _prefilterShader.Unbind();
            }
        }

        CommonShapes.CubeVertexArray?.Unbind();
        _prefilterBuffer.Unbind();
    }

    public void Unload()
    {
        GL.ARB.MakeTextureHandleNonResidentARB(IrradianceBindlessHandle);
        GL.ARB.MakeTextureHandleNonResidentARB(PrefilterBindlessHandle);

        _cubemapRenderTarget.Unload();
        _irradianceRenderTarget.Unload();
        _prefilterRenderTarget.Unload();

        _cubemapBuffer.Unload();
        _prefilterBuffer.Unload();
        _irradianceBuffer.Unload();

        _prefilterShader.Unload();
        _irradianceShader.Unload();
    }
}

public class ImageBasedLighting
{
    public List<LightProbe> Probes { get; } = new();
    private int _currentProbe;
    private readonly LightProbe _playerProbe;

    public readonly ShaderStorageBuffer<LightProbes> LightProbesSsbo;
    public const int max_probes = 512;

    private bool _renderedLastFrame;
    public ImageBasedLighting()
    {
        LightProbesSsbo = new ShaderStorageBuffer<LightProbes>("lightProbesSSBO", new LightProbes());

        _playerProbe = AddProbe();
    }

    public LightProbe AddProbe()
    {
        var probe = new LightProbe(Probes.Count);
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
        var stopwatch = Stopwatch.StartNew();

        if (ConVarStorage.Get<bool>("mat_ibl_enabled"))
        {
            // only render every second frame
            if (_renderedLastFrame)
            {
                _renderedLastFrame = false;
                return;
            }

            _renderedLastFrame = true;

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

            if (_currentProbe >= Probes.Count)
                _currentProbe = 0;

            var lightProbe = Probes[_currentProbe];
            if (lightProbe == _playerProbe)
                lightProbe.Position = Engine.MainViewport.Position;

            lightProbe.RenderCubemap(sky);
            lightProbe.RenderIrradience();

            if (ConVarStorage.Get<bool>("mat_ibl_prefilter"))
                lightProbe.RenderPrefilter();

            _currentProbe++;

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

        PerformanceMeasurment.Add("IBL.Frame", stopwatch.Elapsed.TotalMilliseconds);
    }

    public void Unload()
    {
        foreach (var lightProbe in Probes)
        {
            lightProbe.Unload();
        }
    }
}