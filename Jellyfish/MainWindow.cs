using System;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Jellyfish;

public class MainWindow : GameWindow
{
    public MainWindow() : base(
        new GameWindowSettings { UpdateFrequency = 0.0, Win32SuspendTimerOnDrag = true }, 
        new NativeWindowSettings { APIVersion = new Version(4, 1), Title = "Jellyfish" })
    {
        var config = Settings.Instance;

        ClientSize = config.Video.WindowSize;
        WindowState = config.Video.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;
        CenterWindow();

        Load += OnFinishedLoading;
    }

    protected override void OnLoad()
    {
        Engine.Self.Load();
        base.OnLoad();
    }
    
    private void OnFinishedLoading()
    {
        Engine.Self.OnLoadingFinished();
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        Engine.Self.RenderFrame(e);

        base.OnRenderFrame(e);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        Engine.Self.UpdateFrame(e);

        var config = Settings.Instance.Video;

        // allow some tolerance because graphics apis are funny
        // TODO: signal from the config that we need a resolution change instead of testing every frame
        if (Math.Abs(config.WindowSize.X - ClientSize.X) > 20 ||
            Math.Abs(config.WindowSize.Y - ClientSize.Y) > 20)
        {
            ClientSize = config.WindowSize;
        }

        if (config.Fullscreen && WindowState != WindowState.Fullscreen)
        {
            WindowState = WindowState.Fullscreen;
        }

        if (!config.Fullscreen && WindowState == WindowState.Fullscreen)
        {
            WindowState = WindowState.Normal;
        }

        CursorState = !Engine.InputManager.IsControllingCursor ? CursorState.Normal : CursorState.Grabbed;

        base.OnUpdateFrame(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        // todo: refactor through inputmanager
        Engine.ImguiController?.PressChar((char)e.Unicode);
    }
}