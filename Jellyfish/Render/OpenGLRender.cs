using System;
using Jellyfish.Input;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Render;

public class OpenGLRender : IRender, IInputHandler
{
    private PostProcessing? _postProcessing;
    private FrameBuffer? _mainFramebuffer;
    private RenderTarget? _colorRenderTarget;
    private RenderTarget? _depthRenderTarget;
    private Sky? _sky;

    private bool _wireframe;

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
            FramebufferAttachment.ColorAttachment0, PixelType.UnsignedByte, TextureWrapMode.Clamp);

        _depthRenderTarget = new RenderTarget("_rt_Depth", MainWindow.WindowWidth, MainWindow.WindowHeight, PixelFormat.DepthComponent,
            FramebufferAttachment.DepthAttachment, PixelType.UnsignedShort, TextureWrapMode.Clamp);

        if (!_mainFramebuffer.Check())
        {
            throw new Exception("Couldn't create main framebuffer!");
        }

        _mainFramebuffer.Unbind();

        _sky = new Sky();
        _postProcessing = new PostProcessing(_colorRenderTarget.TextureHandle, _depthRenderTarget.TextureHandle);

        InputManager.RegisterInputHandler(this);
    }

    public void Frame()
    {
        if (!IsReady)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            return;
        }

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        LightManager.DrawShadows();

        _mainFramebuffer?.Bind();

        GL.Viewport(0, 0, MainWindow.WindowWidth, MainWindow.WindowHeight);
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.PolygonMode(MaterialFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);

        MeshManager.Draw();
        _sky?.Draw();

        _mainFramebuffer?.Unbind();

        _postProcessing?.Draw();
    }

    public void Unload()
    {
        MeshManager.Unload();
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (keyboardState.IsKeyPressed(Keys.Q))
        {
            _wireframe = !_wireframe;
            return true;
        }

        return false;
    }
}