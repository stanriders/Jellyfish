using Jellyfish.Console;
using Jellyfish.Input;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Lighting;
using Jellyfish.Render.Screenspace;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Jellyfish.Render;

public class OpenGLRender : IRender, IInputHandler
{
    private FinalOut? _outRender;
    private FrameBuffer? _mainFramebuffer;
    private RenderTarget? _colorRenderTarget;
    private RenderTarget? _depthRenderTarget;
    private Sky? _sky;
    private GBuffer? _gBuffer;
    private readonly Camera _camera;

    private readonly List<ScreenspaceEffect> _screenspaceEffects = new();

    private bool _wireframe;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly GLDebugProc _debugProc; // if this delegate doesn't have a reference it gets GC'd after the first call

    public bool IsReady { get; set; }
    public bool NeedToRecreateBuffers { get; set; }

    public OpenGLRender()
    {
#if DEBUG
        GL.Enable(EnableCap.DebugOutput);
        _debugProc = DebugMessage;
        GL.DebugMessageCallback(_debugProc, nint.Zero);
#endif
        GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _camera = new Camera();
    }

    public void LoadScreenspaceEffects()
    {
        var effects = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsPublic: true, IsAbstract: false } && typeof(ScreenspaceEffect).IsAssignableFrom(x));

        foreach (var effectType in effects)
        {
            if (Activator.CreateInstance(effectType) is ScreenspaceEffect panel)
            {
                _screenspaceEffects.Add(panel);
            }
            else
            {
                Log.Context(this).Error("Can't create screenspace effect {Type}", effectType.Name);
            }
        }
    }

    public void CreateBuffers()
    {
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.FramebufferSrgb);

        _mainFramebuffer = new FrameBuffer();
        _mainFramebuffer.Bind();

        _colorRenderTarget = new RenderTarget("_rt_Color", MainWindow.WindowWidth, MainWindow.WindowHeight, SizedInternalFormat.Rgb16f, FramebufferAttachment.ColorAttachment0, TextureWrapMode.ClampToEdge, levels: 11);
        _depthRenderTarget = new RenderTarget("_rt_Depth", MainWindow.WindowWidth, MainWindow.WindowHeight, SizedInternalFormat.DepthComponent24, FramebufferAttachment.DepthAttachment, TextureWrapMode.ClampToEdge);

        if (!_mainFramebuffer.Check())
        {
            throw new Exception("Couldn't create main framebuffer!");
        }

        _mainFramebuffer.Unbind();

        GL.Disable(EnableCap.FramebufferSrgb);

        _gBuffer = new GBuffer(_depthRenderTarget);
        _sky = new Sky();
        LoadScreenspaceEffects();
        _outRender = new FinalOut();

        InputManager.RegisterInputHandler(this);
    }

    public void Frame()
    {
        if (NeedToRecreateBuffers)
        {
            RecreateRenderTargets();
            return;
        }

        if (!IsReady)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            return;
        }

        _camera.Think();

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        _gBuffer?.GeometryPass();
        LightManager.DrawShadows();

        _mainFramebuffer?.Bind();

        GL.Viewport(0, 0, MainWindow.WindowWidth, MainWindow.WindowHeight);
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        GL.PolygonMode(TriangleFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);

        _sky?.Draw();
        MeshManager.Draw();

        _mainFramebuffer?.Unbind();

        foreach (var effect in _screenspaceEffects)
        {
            effect.Draw();
        }

        _outRender?.Draw();
    }

    public void Unload()
    {
        MeshManager.Unload();

        _sky?.Unload();
        foreach (var effect in _screenspaceEffects)
        {
            effect.Unload();
        }
        _outRender?.Unload();

        _gBuffer?.Unload();
        _colorRenderTarget?.Unload();
        _depthRenderTarget?.Unload();
        _mainFramebuffer?.Unload();
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

    private unsafe void DebugMessage(DebugSource source, DebugType type, uint id, DebugSeverity severity, int length, nint message, nint userParam)
    {
        if (length <= 0)
            return;

        if (id == 131169 || id == 131185 || id == 131218 || id == 131204) 
            return;

        var decodedMessage = Encoding.UTF8.GetString(new Span<byte>(message.ToPointer(), length));
        switch (severity)
        {
            case DebugSeverity.DebugSeverityNotification:
                Log.Context("OpenGL").Debug("{Source} {Type} {Id}: {Message}", source, type, id, decodedMessage);
                break;
            case DebugSeverity.DebugSeverityHigh:
                Log.Context("OpenGL").Error("{Source} {Type} {Id}: {Message}", source, type, id, decodedMessage);
                break;
            case DebugSeverity.DebugSeverityMedium:
                Log.Context("OpenGL").Warning("{Source} {Type} {Id}: {Message}", source, type, id, decodedMessage);
                break;
            case DebugSeverity.DebugSeverityLow:
                Log.Context("OpenGL").Information("{Source} {Type} {Id}: {Message}", source, type, id, decodedMessage);
                break;
        }
    }

    public void RecreateRenderTargets()
    {
        Log.Context(this).Warning("Recreating render buffers!");

        _sky?.Unload();
        foreach (var effect in _screenspaceEffects)
        {
            effect.Unload();
        }
        _screenspaceEffects.Clear();
        _outRender?.Unload();

        _gBuffer?.Unload();
        _colorRenderTarget?.Unload();
        _depthRenderTarget?.Unload();
        _mainFramebuffer?.Unload();

        CreateBuffers();

        NeedToRecreateBuffers = false;
    }
}