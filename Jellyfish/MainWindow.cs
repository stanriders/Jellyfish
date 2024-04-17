using Jellyfish.Render;
using Jellyfish.UI;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Jellyfish;

public class MainWindow : GameWindow
{
    private InputHandler _inputHandler = null!;
    private OpenGLRender _render = null!;
    private ImguiController _imguiController = null!;

    public MainWindow(int width, int height, string title) : base(
        new GameWindowSettings { UpdateFrequency = 144.0 }, NativeWindowSettings.Default)
    {
        WindowHeight = height;
        WindowWidth = width;

        ClientSize = new Vector2i(width, height);
        Title = title;
    }

    public static int WindowX { get; set; }
    public static int WindowY { get; set; }
    public static int WindowWidth { get; set; }
    public static int WindowHeight { get; set; }

    protected override void OnLoad()
    {
        _render = new OpenGLRender();
        _inputHandler = new InputHandler();
        _imguiController = new ImguiController();

        base.OnLoad();
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
        UiManager.Frame();

        if (!IsFocused)
            return;

        EntityManager.Frame();

        WindowX = ClientSize.X;
        WindowY = ClientSize.Y;

        _inputHandler.Frame(KeyboardState, MouseState, (float)e.Time);
        CursorState = !_inputHandler.IsControllingCursor ? CursorState.Normal : CursorState.Grabbed;

        base.OnUpdateFrame(e);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        if (!IsFocused)
            return;

        _inputHandler.OnMouseMove(e);

        base.OnMouseMove(e);
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
        EntityManager.Unload();
        _render.Unload();

        base.OnUnload();
    }
}