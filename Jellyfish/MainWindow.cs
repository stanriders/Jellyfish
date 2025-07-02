using System;
using System.Diagnostics;
using ImGuiNET;
using Jellyfish.Audio;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Input;
using Jellyfish.Render;
using Jellyfish.UI;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System.Threading;
using Jellyfish.Debug;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish;

public class MainWindow : GameWindow, IInputHandler
{
    private readonly OpenGLRender _render;
    private InputManager _inputHandler = null!;
    private ImguiController? _imguiController;
    private EntityManager _entityManager = null!;
    private UiManager _uiManager = null!;
    private AudioManager _audioManager = null!;
    private PhysicsManager _physicsManager = null!;

    private int _loadingStep;

    public MainWindow() : base(
        new GameWindowSettings { UpdateFrequency = 0.0 }, NativeWindowSettings.Default)
    {
        var config = Settings.Instance;
        WindowHeight = config.Video.WindowSize.Y;
        WindowWidth = config.Video.WindowSize.X;

        ClientSize = config.Video.WindowSize;
        WindowState = config.Video.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;
        Title = "Jellyfish";

        _render = new OpenGLRender();
        Render();

        CenterWindow();

        Load += OnFinishedLoading;
    }
    
    public static int WindowWidth { get; set; }
    public static int WindowHeight { get; set; }
    public static double Frametime { get; set; }
    public static bool ShouldQuit { get; set; }
    public static string? QueuedMap { private get; set; }
    public static string? CurrentMap { get; private set; }
    public static bool Loaded => CurrentMap != null;

    public static bool Paused { get; set; }

    protected override void OnLoad()
    {
        Log.Context(this).Information("Loading..."); 

        _inputHandler = new InputManager();
        _imguiController = new ImguiController();

        InputManager.RegisterInputHandler(this);

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
            Log.Context(this).Debug("Waiting for physics to start...");
        }

        Log.Context(this).Information("Finished loading!");

#if DEBUG
        QueuedMap = "maps/test.json";
        Paused = true;
#endif
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        var stopwatch = Stopwatch.StartNew();

        RenderScheduler.Run();
        Render();

        base.OnRenderFrame(e);

        PerformanceMeasurment.Add("RenderTotal", stopwatch.Elapsed.TotalMilliseconds);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        var stopwatch = Stopwatch.StartNew();

        Frametime = e.Time;

        if (ShouldQuit)
        {
            Close();
            return;
        }

        if (QueuedMap != null)
        {
            LoadMap(QueuedMap);
            _loadingStep = 0;
            QueuedMap = null;
        }

        // we want to update ui regardless of focus otherwise it disappears
        _imguiController?.Update(WindowWidth, WindowHeight);
        _uiManager.Frame(e.Time);
        _inputHandler.Frame(KeyboardState, MouseState, (float)e.Time);

        if (!Loaded)
        {
            base.OnUpdateFrame(e);
            return;
        }

        var config = Settings.Instance.Video;
        // allow some tolerance because graphics apis are funny
        // TODO: signal from the config that we need a resolution change instead of testing every frame
        if (Math.Abs(config.WindowSize.X - ClientSize.X) > 20 ||
            Math.Abs(config.WindowSize.Y - ClientSize.Y) > 20)
        {
            ClientSize = config.WindowSize;
            WindowHeight = config.WindowSize.Y;
            WindowWidth = config.WindowSize.X;
            _render.NeedToRecreateBuffers = true;
        }

        if (config.Fullscreen && WindowState != WindowState.Fullscreen)
        {
            WindowState = WindowState.Fullscreen;
        }

        if (!config.Fullscreen && WindowState == WindowState.Fullscreen)
        {
            WindowState = WindowState.Normal;
        }
        CursorState = !Camera.Instance.IsControllingCursor ? CursorState.Normal : CursorState.Grabbed;

        if (!IsFocused && !Paused)
            Paused = true;

        _physicsManager.ShouldSimulate = !Paused;
        _entityManager.Frame((float)e.Time);

        base.OnUpdateFrame(e);

        PerformanceMeasurment.Add("UpdateTotal", stopwatch.Elapsed.TotalMilliseconds);
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

    private void LoadMap(string map)
    {
        _physicsManager.ShouldSimulate = false;
        _render.IsReady = false;

        UpdateLoadingScreen("Cleaning up entities...");
        _entityManager.Unload();
        _audioManager.ClearScene();

        UpdateLoadingScreen($"Loading map '{map}'...");
        MapLoader.Load(map);

        UpdateLoadingScreen("Creating player...");

        var player = EntityManager.FindEntity("player");
        if (player == null)
            player = EntityManager.CreateEntity("player");

        Camera.Instance.AspectRatio = WindowWidth / (float)WindowHeight;
        Camera.Instance.Position = player?.GetPropertyValue<Vector3>("Position") ?? Vector3.Zero;

        UpdateLoadingScreen("Finishing loading...");

        CurrentMap = map;

        _render.IsReady = true;
        _physicsManager.ShouldSimulate = true;
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (keyboardState.IsKeyPressed(Keys.Escape))
        {
            Paused = !Paused;
            return true;
        }

        return false;
    }
}