using Hexa.NET.ImGui;
using Jellyfish.Audio;
using Jellyfish.Console;
using Jellyfish.Debug;
using Jellyfish.Entities;
using Jellyfish.Input;
using Jellyfish.Render;
using Jellyfish.UI;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;
using System.Threading;

namespace Jellyfish
{
    public class Engine : IInputHandler, IDisposable
    {
        private readonly MainWindow _mainWindow;
        private readonly OpenGLRender _render;
        private ShaderManager _shaderManager = null!;
        private InputManager _inputHandler = null!;
        private ImguiController? _imguiController;
        private EntityManager _entityManager = null!;
        private UiManager _uiManager = null!;
        private AudioManager _audioManager = null!;
        private PhysicsManager _physicsManager = null!;
        private Viewport _viewport = null!;
        private MeshManager _meshManager = null!;
        private TextureManager _textureManager = null!;

        private int _loadingStep;

        public static Engine Self => instance;
        public static InputManager InputManager => instance._inputHandler;
        public static ImguiController? ImguiController => instance._imguiController;
        public static EntityManager EntityManager => instance._entityManager;
        public static UiManager UiManager => instance._uiManager;
        public static AudioManager AudioManager => instance._audioManager;
        public static PhysicsManager PhysicsManager => instance._physicsManager;
        public static Viewport MainViewport => instance._viewport;
        public static MeshManager MeshManager => instance._meshManager;
        public static ShaderManager ShaderManager => instance._shaderManager;
        public static TextureManager TextureManager => instance._textureManager;
        public static MainWindow MainWindow => instance._mainWindow;

        public static double Frametime { get; set; }
        public static bool ShouldQuit { get; set; }
        public static string? QueuedMap { private get; set; }
        public static string? CurrentMap { get; private set; }
        public static bool Loaded => CurrentMap != null;

        public static bool Paused { get; set; }

        private static Engine instance = null!;

        public Engine()
        {
            instance = this;

            _mainWindow = new MainWindow();
            _render = new OpenGLRender();
        }

        public void Run()
        {
            _mainWindow.Run();
        }

        public void Load()
        {
            Log.Context(this).Information("Loading...");

            _inputHandler = new InputManager();
            _shaderManager = new ShaderManager();
            _textureManager = new TextureManager();
            _imguiController = new ImguiController();

            UpdateLoadingScreen("Loading systems...");
            _meshManager = new MeshManager();

            InputManager.RegisterInputHandler(this);

            UpdateLoadingScreen("Loading UI...");
            _uiManager = new UiManager();

            UpdateLoadingScreen("Starting entity manager...");
            _entityManager = new EntityManager();

            UpdateLoadingScreen("Starting audio...");
            _audioManager = new AudioManager();

            UpdateLoadingScreen("Creating rendering buffers...");
            _viewport = new Viewport { Size = _mainWindow.ClientSize };
            _render.CreateBuffers();

            UpdateLoadingScreen("Starting physics...");
            _physicsManager = new PhysicsManager();
        }

        public void OnLoadingFinished()
        {
            while (!_physicsManager.IsReady)
            {
                Thread.Sleep(100);
                Log.Context(this).Debug("Waiting for physics to start...");
            }

            Log.Context(this).Information("Finished loading!");

#if DEBUG
            QueuedMap = "test";
            Paused = true;
#endif
        }

        public void UpdateFrame(FrameEventArgs e)
        {
            var stopwatch = Stopwatch.StartNew();

            Frametime = e.Time;
            _viewport.Think();

            if (ShouldQuit)
            {
                _mainWindow.Close();
                return;
            }

            if (QueuedMap != null)
            {
                LoadMap(QueuedMap);
                _loadingStep = 0;
                QueuedMap = null;
            }

            // we want to update ui regardless of focus otherwise it disappears
            _imguiController?.Update(_mainWindow.ClientSize.X, _mainWindow.ClientSize.Y);
            _uiManager.Frame(e.Time);
            _inputHandler.Frame(_mainWindow.KeyboardState, _mainWindow.MouseState, (float)e.Time);

            if (!Loaded)
            {
                return;
            }

            var config = Settings.Instance.Video;
            // allow some tolerance because graphics apis are funny
            // TODO: signal from the config that we need a resolution change instead of testing every frame
            if (Math.Abs(config.WindowSize.X - _viewport.Size.X) > 20 ||
                Math.Abs(config.WindowSize.Y - _viewport.Size.Y) > 20)
            {
                _render.NeedToRecreateBuffers = true;
            }

            if (!_mainWindow.IsFocused && !Paused)
                Paused = true;

            _physicsManager.ShouldSimulate = !Paused;
            _entityManager.Frame((float)e.Time);

            PerformanceMeasurment.Add("UpdateTotal", stopwatch.Elapsed.TotalMilliseconds);
        }

        public void RenderFrame(FrameEventArgs e)
        {
            PerformanceMeasurment.Reset("DrawCalls");
            var stopwatch = Stopwatch.StartNew();

            RenderScheduler.Run();
            Render();

            PerformanceMeasurment.Add("RenderTotal", stopwatch.Elapsed.TotalMilliseconds);
        }

        private void Render()
        {
            _render.Frame();
            _imguiController?.Render();

            var stopwatch = Stopwatch.StartNew();
            _mainWindow.SwapBuffers();
            PerformanceMeasurment.Add("SwapBuffers", stopwatch.Elapsed.TotalMilliseconds);
        }

        private void UpdateLoadingScreen(string text = "Loading...")
        {
            _imguiController?.Update(_mainWindow.ClientSize.X, _mainWindow.ClientSize.Y);

            var windowFlags = ImGuiWindowFlags.NoDecoration |
                              ImGuiWindowFlags.AlwaysAutoResize |
                              ImGuiWindowFlags.NoSavedSettings |
                              ImGuiWindowFlags.NoFocusOnAppearing |
                              ImGuiWindowFlags.NoNav |
                              ImGuiWindowFlags.NoMove;

            const int loadingSteps = 9;
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

            var player = EntityManager.FindEntity("player") ?? EntityManager.CreateEntity("player");

            _viewport.Position = player?.GetPropertyValue<Vector3>("Position") ?? Vector3.Zero;

            UpdateLoadingScreen("Finishing loading...");

            CurrentMap = map;

            _render.IsReady = true;
            _physicsManager.ShouldSimulate = true;
            _loadingStep = 0;
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

        public void Dispose()
        {
            _meshManager.Unload();
            _entityManager.Unload();
            _render.Unload();
            _imguiController?.Dispose();
            _audioManager.Unload();
            _physicsManager.Unload();
            _mainWindow.Dispose();
        }
    }
}
