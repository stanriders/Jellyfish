using Jellyfish.Console;
using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders.IBL;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Jellyfish.Render.Lighting;

public class IblEnabled() : ConVar<bool>("mat_ibl_enabled", false);
public class IblRenderWorld() : ConVar<bool>("mat_ibl_render_world", false);

public class ImageBasedLighting
{
    private readonly FrameBuffer _cubemapBuffer;
    private readonly Texture _cubemapRenderTarget;

    private readonly FrameBuffer _irradianceBuffer;
    private readonly Texture _irradianceRenderTarget;
    private readonly Irradiance _irradianceShader;

    private readonly FrameBuffer _prefilterBuffer;
    private readonly RenderBuffer _prefilterRenderbuffer;
    private readonly Texture _prefilterRenderTarget;
    private readonly Prefiltering _prefilterShader;

    private readonly VertexBuffer _cubeVbo;
    private readonly VertexArray _cubeVao;

    private bool _renderedLastFrame;

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

    private const int size = 128;
    private const int irradiance_size = 32;

    public ImageBasedLighting()
    {
        _cubeVbo = new VertexBuffer("IrradianceCube", CommonShapes.CubeFloat, 3 * sizeof(float));
        _cubeVao = new VertexArray(_cubeVbo, null);

        GL.EnableVertexArrayAttrib(_cubeVao.Handle, 0);
        GL.VertexArrayAttribFormat(_cubeVao.Handle, 0, 3, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(_cubeVao.Handle, 0, 0);

        #region cubemap
        _cubemapBuffer = new FrameBuffer();
        _cubemapBuffer.Bind();

        RenderBuffer.Create(InternalFormat.DepthComponent, FramebufferAttachment.DepthAttachment, size, size);

        _cubemapRenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = "_rt_EnvironmentMap",
            Type = TextureTarget.TextureCubeMap,
            WrapMode = TextureWrapMode.ClampToEdge,
            MinFiltering = TextureMinFilter.Nearest,
            MagFiltering = TextureMagFilter.Nearest,
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
        _irradianceShader = new Irradiance();
        _irradianceBuffer = new FrameBuffer();
        _irradianceBuffer.Bind();
        RenderBuffer.Create(InternalFormat.DepthComponent, FramebufferAttachment.DepthAttachment, irradiance_size, irradiance_size);

        _irradianceRenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = "_rt_Irradiance",
            Type = TextureTarget.TextureCubeMap,
            WrapMode = TextureWrapMode.ClampToEdge,
            MinFiltering = TextureMinFilter.Nearest,
            MagFiltering = TextureMagFilter.Nearest,
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
        #endregion

        #region prefilter

        _prefilterShader = new Prefiltering();
        _prefilterBuffer = new FrameBuffer();
        _prefilterBuffer.Bind();

        _prefilterRenderbuffer = new RenderBuffer(InternalFormat.DepthComponent, FramebufferAttachment.DepthAttachment, size, size);

        _prefilterRenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = "_rt_Prefilter",
            Type = TextureTarget.TextureCubeMap,
            WrapMode = TextureWrapMode.ClampToEdge,
            MaxLevels = null,
            MinFiltering = TextureMinFilter.LinearMipmapLinear,
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
        
        #endregion
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

            RenderCubemap(sky);
            RenderIrradience();
            RenderPrefilter();

            Engine.MainViewport.ViewMatrixOverride = null;
            Engine.MainViewport.ProjectionMatrixOverride = null;
        }
        PerformanceMeasurment.Add("IBL.Frame", stopwatch.Elapsed.TotalMilliseconds);
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
                Engine.MainViewport.ViewMatrixOverride = Matrix4.LookAt(Engine.MainViewport.Position, Engine.MainViewport.Position + _cubemapViews[i].target, _cubemapViews[i].up);
                Engine.MeshManager.Draw(false, frustum: Engine.MainViewport.GetFrustum());
            }
        }

        _cubemapBuffer.Unbind();
    }

    public void RenderIrradience()
    {
        GL.Viewport(0, 0, irradiance_size, irradiance_size);

        _irradianceBuffer.Bind();
        _cubeVao.Bind();

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

        _cubeVao.Unbind();
        _irradianceBuffer.Unbind();
    }

    public void RenderPrefilter()
    {
        GL.Viewport(0, 0, size, size);

        _prefilterBuffer.Bind();
        _cubeVao.Bind();

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

        _cubeVao.Unbind();
        _prefilterBuffer.Unbind();
    }

    public void Unload()
    {
        _cubemapRenderTarget.Unload();
        _irradianceRenderTarget.Unload();
        _prefilterRenderTarget.Unload();

        _cubemapBuffer.Unload();
        _prefilterBuffer.Unload();
        _irradianceBuffer.Unload();

        _cubeVao.Unload();
        _cubeVbo.Unload();
    }
}