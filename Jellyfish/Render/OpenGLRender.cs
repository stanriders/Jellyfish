using System;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
namespace Jellyfish.Render;

public class OpenGLRender : IRender
{
    private readonly PostProcessing _postProcessing;
    private readonly FrameBuffer _mainFramebuffer;
    private readonly RenderTarget _colorRenderTarget;
    private readonly RenderTarget _depthRenderTarget;

    public OpenGLRender()
    {
        GL.Enable(EnableCap.CullFace);

        _mainFramebuffer = new FrameBuffer();
        _mainFramebuffer.Bind();

        RenderBuffer.Create(RenderbufferStorage.StencilIndex8, FramebufferAttachment.Stencil, 
            MainWindow.WindowWidth, MainWindow.WindowHeight);

        _colorRenderTarget = new RenderTarget(MainWindow.WindowWidth, MainWindow.WindowHeight, PixelFormat.Rgb,
            FramebufferAttachment.ColorAttachment0, PixelType.UnsignedByte);

        _depthRenderTarget = new RenderTarget(MainWindow.WindowWidth, MainWindow.WindowHeight, PixelFormat.DepthComponent,
            FramebufferAttachment.DepthAttachment, PixelType.UnsignedShort);

        if (!_mainFramebuffer.Check())
        {
            throw new Exception("Couldn't create main framebuffer!");
        }

        _mainFramebuffer.Unbind();

        _postProcessing = new PostProcessing(_colorRenderTarget.TextureHandle, _depthRenderTarget.TextureHandle);
    }

    public void Frame()
    {
        _mainFramebuffer.Bind();

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        GL.Viewport(0, 0, MainWindow.WindowWidth, MainWindow.WindowHeight);
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        MeshManager.Draw();

        _mainFramebuffer.Unbind();

        _postProcessing.Draw();
    }

    public void Unload()
    {
        MeshManager.Unload();
    }
}