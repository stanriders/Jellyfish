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
    private InputManager _inputHandler = null!;
    private OpenGLRender _render = null!;
    private ImguiController _imguiController = null!;
    private EntityManager _entityManager = null!;
    private UiManager _uiManager = null!;
    private Camera? _camera;

    public MainWindow(int width, int height, string title) : base(
        new GameWindowSettings { UpdateFrequency = 144.0 }, NativeWindowSettings.Default)
    {
        WindowHeight = height;
        WindowWidth = width;

        ClientSize = new Vector2i(width, height);
        Title = title;

        Load += OnFinishedLoading;
    }

    public static int WindowX { get; set; }
    public static int WindowY { get; set; }
    public static int WindowWidth { get; set; }
    public static int WindowHeight { get; set; }

    protected override void OnLoad()
    {
        Log.Information("[MainWindow] Loading..."); 

        _inputHandler = new InputManager();
        _imguiController = new ImguiController();
        _uiManager = new UiManager();
        _entityManager = new EntityManager();
        
        _camera = EntityManager.CreateEntity("camera") as Camera;
        if (_camera != null)
        {
            _camera.AspectRatio = WindowWidth / (float)WindowHeight;
        }

        base.OnLoad();
    }
    
    private void OnFinishedLoading()
    {
        Log.Information("[MainWindow] Finished loading!");

        MapLoader.Load("maps/test.yml");
        _render = new OpenGLRender();
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        _render.Frame();
        _imguiController.Render();

        SwapBuffers();

        base.OnRenderFrame(e);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        // we want to update ui regardless of focus otherwise it disappears
        _imguiController.Update(WindowWidth, WindowHeight);
        _uiManager.Frame();

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
        _imguiController.Dispose();

        base.OnUnload();
    }
}