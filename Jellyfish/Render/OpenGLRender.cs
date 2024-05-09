using System;
using ImGuiNET;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
namespace Jellyfish.Render;

public class OpenGLRender : IRender
{
    private PostProcessing? _postProcessing;
    private FrameBuffer? _mainFramebuffer;
    private RenderTarget? _colorRenderTarget;
    private RenderTarget? _depthRenderTarget;
    private Sky? _sky;

    public bool IsReady { get; set; }

    public OpenGLRender()
    {
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public void CreateBuffers()
    {
        GL.Enable(EnableCap.CullFace);

        _mainFramebuffer = new FrameBuffer();
        _mainFramebuffer.Bind();

        RenderBuffer.Create(RenderbufferStorage.StencilIndex8, FramebufferAttachment.Stencil,
            MainWindow.WindowWidth, MainWindow.WindowHeight);

        _colorRenderTarget = new RenderTarget("_rt_Color", MainWindow.WindowWidth, MainWindow.WindowHeight, PixelFormat.Rgb,
            FramebufferAttachment.ColorAttachment0, PixelType.UnsignedByte);

        _depthRenderTarget = new RenderTarget("_rt_Depth", MainWindow.WindowWidth, MainWindow.WindowHeight, PixelFormat.DepthComponent,
            FramebufferAttachment.DepthAttachment, PixelType.UnsignedShort);

        if (!_mainFramebuffer.Check())
        {
            throw new Exception("Couldn't create main framebuffer!");
        }

        _mainFramebuffer.Unbind();

        _sky = new Sky();
        _postProcessing = new PostProcessing(_colorRenderTarget.TextureHandle, _depthRenderTarget.TextureHandle);
    }

    public void Frame()
    {
        if (!IsReady)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            return;
        }

        _mainFramebuffer?.Bind();

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        GL.Viewport(0, 0, MainWindow.WindowWidth, MainWindow.WindowHeight);
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        MeshManager.Draw();
        _sky?.Draw();

        _mainFramebuffer?.Unbind();

        _postProcessing?.Draw();
    }

    public void Unload()
    {
        MeshManager.Unload();
    }
}