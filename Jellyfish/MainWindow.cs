using System;
using ImGuiNET;
using Jellyfish.Audio;
using Jellyfish.Entities;
using Jellyfish.Input;
using Jellyfish.Render;
using Jellyfish.UI;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using Serilog;

namespace Jellyfish;

public class MainWindow : GameWindow
{
    private readonly OpenGLRender _render;
    private InputManager _inputHandler = null!;
    private ImguiController? _imguiController;
    private EntityManager _entityManager = null!;
    private UiManager _uiManager = null!;
    private AudioManager _audioManager = null!;
    private Camera? _camera;

    public MainWindow(int width, int height, string title) : base(
        new GameWindowSettings { UpdateFrequency = 0.0 }, NativeWindowSettings.Default)
    {
        WindowHeight = height;
        WindowWidth = width;

        ClientSize = new Vector2i(width, height);
        Title = title;

        _render = new OpenGLRender();
        Render();

        Load += OnFinishedLoading;
    }

    public static int WindowX { get; set; }
    public static int WindowY { get; set; }
    public static int WindowWidth { get; set; }
    public static int WindowHeight { get; set; }
    public static double Frametime { get; set; }

    protected override void OnLoad()
    {
        Log.Information("[MainWindow] Loading..."); 

        _inputHandler = new InputManager();
        _imguiController = new ImguiController();
        _uiManager = new UiManager();
        _entityManager = new EntityManager();
        _audioManager = new AudioManager();

        _camera = EntityManager.CreateEntity("camera") as Camera;
        if (_camera != null)
        {
            _camera.AspectRatio = WindowWidth / (float)WindowHeight;
        }

        _render.CreateBuffers();

        base.OnLoad();
    }
    
    private void OnFinishedLoading()
    {
        Log.Information("[MainWindow] Finished loading!");

        MapLoader.Load("maps/test.json");
        _render.IsReady = true;
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        Render();

        base.OnRenderFrame(e);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        Frametime = e.Time;

        // we want to update ui regardless of focus otherwise it disappears
        _imguiController?.Update(WindowWidth, WindowHeight);
        _uiManager.Frame();

        _audioManager.Update();

        if (!IsFocused)
            return;

        _entityManager.Frame();

        WindowX = ClientSize.X;
        WindowY = ClientSize.Y;

        _inputHandler.Frame(KeyboardState, MouseState, (float)e.Time);
        CursorState = !_camera?.IsControllingCursor ?? false ? CursorState.Normal : CursorState.Grabbed;

        base.OnUpdateFrame(e);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        GL.Viewport(0, 0, e.Width, e.Height);

        WindowHeight = e.Height;
        WindowWidth = e.Width;

        base.OnResize(e);
    }

    protected override void OnUnload()
    {
        _entityManager.Unload();
        _render.Unload();
        _imguiController?.Dispose();

        base.OnUnload();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        // todo: refactor through inputmanager
        _imguiController?.PressChar((char)e.Unicode);
    }

    private void Render()
    {
        _render.Frame();
        _imguiController?.Render();

        SwapBuffers();
    }
}