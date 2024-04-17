using System;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish;

public class InputHandler
{
    private readonly Camera? _camera;
    private const float camera_speed = 16.0f;
    private const float sensitivity = 0.2f;

    private bool _wireframe;
    
    public bool IsControllingCursor { get; set; }

    public InputHandler()
    {
        _camera ??= EntityManager.CreateEntity("camera") as Camera;
        if (_camera != null)
        {
            _camera.AspectRatio = MainWindow.WindowWidth / (float)MainWindow.WindowHeight;
        }
    }

    public void Frame(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        var input = keyboardState;
        var mouseinput = mouseState;

        if (input.IsKeyDown(Keys.Escape)) 
            Environment.Exit(0);

        if (HandleImgui(keyboardState, mouseState))
        {
            return;
        }

        if (_camera != null)
        {
            var cameraSpeed = input.IsKeyDown(Keys.LeftShift) ? camera_speed * 4 : camera_speed;

            if (mouseinput.IsButtonDown(MouseButton.Left))
            {
                if (input.IsKeyDown(Keys.W))
                    _camera.Position += _camera.Front * cameraSpeed * frameTime; // Forward 
                if (input.IsKeyDown(Keys.S))
                    _camera.Position -= _camera.Front * cameraSpeed * frameTime; // Backwards
                if (input.IsKeyDown(Keys.A))
                    _camera.Position -= _camera.Right * cameraSpeed * frameTime; // Left
                if (input.IsKeyDown(Keys.D))
                    _camera.Position += _camera.Right * cameraSpeed * frameTime; // Right
                if (input.IsKeyDown(Keys.Space))
                    _camera.Position += _camera.Up * cameraSpeed * frameTime; // Up 
                if (input.IsKeyDown(Keys.LeftControl))
                    _camera.Position -= _camera.Up * cameraSpeed * frameTime; // Down

                if (!IsControllingCursor)
                    IsControllingCursor = true;
            }
            else
            {
                if (IsControllingCursor)
                    IsControllingCursor = false;
            }
        }

        if (input.IsKeyPressed(Keys.Q))
        {
            _wireframe = !_wireframe;
            GL.PolygonMode(MaterialFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);
        }
    }

    private bool HandleImgui(KeyboardState keyboardState, MouseState mouseState)
    {
        var io = ImGui.GetIO();
        
        io.MouseDown[0] = mouseState[MouseButton.Left];
        io.MouseDown[1] = mouseState[MouseButton.Right];
        io.MouseDown[2] = mouseState[MouseButton.Middle];
        
        io.MousePos = new System.Numerics.Vector2(mouseState.X, mouseState.Y);

        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if (key == Keys.Unknown)
            {
                continue;
            }
            io.AddKeyEvent(TranslateKeyToImgui(key), keyboardState.IsKeyDown(key));
        }

        io.KeyCtrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
        io.KeyShift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        io.KeySuper = keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);

        return io.WantCaptureMouse || io.WantCaptureKeyboard;
    }

    public void OnMouseMove(MouseMoveEventArgs e)
    {
        if (IsControllingCursor && _camera != null)
        {
            _camera.Yaw += e.DeltaX * sensitivity;
            _camera.Pitch -= e.DeltaY * sensitivity;
        }
    }

    public static ImGuiKey TranslateKeyToImgui(Keys key)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
            return key - Keys.D0 + ImGuiKey._0;

        if (key >= Keys.A && key <= Keys.Z)
            return key - Keys.A + ImGuiKey.A;

        if (key >= Keys.KeyPad0 && key <= Keys.KeyPad9)
            return key - Keys.KeyPad0 + ImGuiKey.Keypad0;

        if (key >= Keys.F1 && key <= Keys.F24)
            return key - Keys.F1 + ImGuiKey.F24;

        switch (key)
        {
            case Keys.Tab: return ImGuiKey.Tab;
            case Keys.Left: return ImGuiKey.LeftArrow;
            case Keys.Right: return ImGuiKey.RightArrow;
            case Keys.Up: return ImGuiKey.UpArrow;
            case Keys.Down: return ImGuiKey.DownArrow;
            case Keys.PageUp: return ImGuiKey.PageUp;
            case Keys.PageDown: return ImGuiKey.PageDown;
            case Keys.Home: return ImGuiKey.Home;
            case Keys.End: return ImGuiKey.End;
            case Keys.Insert: return ImGuiKey.Insert;
            case Keys.Delete: return ImGuiKey.Delete;
            case Keys.Backspace: return ImGuiKey.Backspace;
            case Keys.Space: return ImGuiKey.Space;
            case Keys.Enter: return ImGuiKey.Enter;
            case Keys.Escape: return ImGuiKey.Escape;
            case Keys.Apostrophe: return ImGuiKey.Apostrophe;
            case Keys.Comma: return ImGuiKey.Comma;
            case Keys.Minus: return ImGuiKey.Minus;
            case Keys.Period: return ImGuiKey.Period;
            case Keys.Slash: return ImGuiKey.Slash;
            case Keys.Semicolon: return ImGuiKey.Semicolon;
            case Keys.Equal: return ImGuiKey.Equal;
            case Keys.LeftBracket: return ImGuiKey.LeftBracket;
            case Keys.Backslash: return ImGuiKey.Backslash;
            case Keys.RightBracket: return ImGuiKey.RightBracket;
            case Keys.GraveAccent: return ImGuiKey.GraveAccent;
            case Keys.CapsLock: return ImGuiKey.CapsLock;
            case Keys.ScrollLock: return ImGuiKey.ScrollLock;
            case Keys.NumLock: return ImGuiKey.NumLock;
            case Keys.PrintScreen: return ImGuiKey.PrintScreen;
            case Keys.Pause: return ImGuiKey.Pause;
            case Keys.KeyPadDecimal: return ImGuiKey.KeypadDecimal;
            case Keys.KeyPadDivide: return ImGuiKey.KeypadDivide;
            case Keys.KeyPadMultiply: return ImGuiKey.KeypadMultiply;
            case Keys.KeyPadSubtract: return ImGuiKey.KeypadSubtract;
            case Keys.KeyPadAdd: return ImGuiKey.KeypadAdd;
            case Keys.KeyPadEnter: return ImGuiKey.KeypadEnter;
            case Keys.KeyPadEqual: return ImGuiKey.KeypadEqual;
            case Keys.LeftShift: return ImGuiKey.LeftShift;
            case Keys.LeftControl: return ImGuiKey.LeftCtrl;
            case Keys.LeftAlt: return ImGuiKey.LeftAlt;
            case Keys.LeftSuper: return ImGuiKey.LeftSuper;
            case Keys.RightShift: return ImGuiKey.RightShift;
            case Keys.RightControl: return ImGuiKey.RightCtrl;
            case Keys.RightAlt: return ImGuiKey.RightAlt;
            case Keys.RightSuper: return ImGuiKey.RightSuper;
            case Keys.Menu: return ImGuiKey.Menu;
            default: return ImGuiKey.None;
        }
    }
}