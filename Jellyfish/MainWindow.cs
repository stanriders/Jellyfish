using System.Numerics;
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
using System.Threading;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace Jellyfish;

public class MainWindow : GameWindow
{
    private readonly OpenGLRender _render;
    private InputManager _inputHandler = null!;
    private ImguiController? _imguiController;
    private EntityManager _entityManager = null!;
    private UiManager _uiManager = null!;
    private AudioManager _audioManager = null!;
    private PhysicsManager _physicsManager = null!;
    private Camera? _camera;

    private int _loadingStep;

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

        UpdateLoadingScreen("Loading UI...");
        _uiManager = new UiManager();

        UpdateLoadingScreen("Starting entity manager...");
        _entityManager = new EntityManager();

        UpdateLoadingScreen("Starting audio...");
        _audioManager = new AudioManager();

        UpdateLoadingScreen("Creating rendering buffers...");
        _render.CreateBuffers();

        UpdateLoadingScreen("Starting physics...");
        _physicsManager = new PhysicsManager();

        base.OnLoad();
    }
    
    private void OnFinishedLoading()
    {
        while (!_physicsManager.IsReady)
        {
            Thread.Sleep(100);
            Log.Debug("[MainWindow] Waiting for physics to start...");
        }

        Log.Information("[MainWindow] Finished loading!");

        UpdateLoadingScreen("Creating player...");
        _camera = EntityManager.CreateEntity("camera") as Camera;
        if (_camera != null)
        {
            _camera.AspectRatio = WindowWidth / (float)WindowHeight;
            _camera.SetPropertyValue("Position", new Vector3(40, 20, 20));
        }

        var mapName = "maps/test.json";
        UpdateLoadingScreen($"Loading map '{mapName}'...");
        MapLoader.Load(mapName);

        UpdateLoadingScreen("Finishing loading...");
        _render.IsReady = true;
        _physicsManager.ShouldSimulate = true;
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

        if (!IsFocused)
        {
            _physicsManager.ShouldSimulate = false;
            return;
        }

        _physicsManager.ShouldSimulate = true;
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
        _audioManager.Unload();
        _physicsManager.Unload();

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

    private void UpdateLoadingScreen(string text = "Loading...")
    {
        _imguiController?.Update(WindowWidth, WindowHeight);

        var windowFlags = ImGuiWindowFlags.NoDecoration |
                          ImGuiWindowFlags.AlwaysAutoResize |
                          ImGuiWindowFlags.NoSavedSettings |
                          ImGuiWindowFlags.NoFocusOnAppearing |
                          ImGuiWindowFlags.NoNav |
                          ImGuiWindowFlags.NoMove;

        const int loadingSteps = 8;
        const float fracIncrease = 1.0f / loadingSteps;

        const int pad = 10;
        const int heigth = 60;

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.WorkPos.X + pad, viewport.WorkSize.Y - heigth - pad), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(viewport.WorkSize.X - pad * 2, heigth));
        ImGui.SetNextWindowBgAlpha(0.2f);

        if (ImGui.Begin("LoadingScreen", windowFlags))
        {
            ImGui.Text(text);
            ImGui.ProgressBar(fracIncrease * _loadingStep, new System.Numerics.Vector2(viewport.WorkSize.X - pad * 4, 20f));
            ImGui.End();
        }

        Render();

        _loadingStep++;
    }
}