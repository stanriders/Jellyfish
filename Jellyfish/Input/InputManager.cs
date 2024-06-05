using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Input;

public class InputManager
{
    private readonly List<IInputHandler> _inputHandlers = new();
    private bool _inputCaptured;
    private IInputHandler _capturer;

    private static InputManager? instance;

    public InputManager()
    {
        instance = this;
    }

    public static void RegisterInputHandler(IInputHandler inputHandler)
    {
        instance?._inputHandlers.Add(inputHandler);
    }

    public static void CaptureInput(IInputHandler inputHandler)
    {
        if (instance != null)
        {
            instance._inputCaptured = true;
            instance._capturer = inputHandler;
        }
    }

    public static void ReleaseInput(IInputHandler inputHandler)
    {
        if (instance != null && inputHandler == instance._capturer)
            instance._inputCaptured = false;
    }

    public void Frame(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (_inputCaptured)
        {
            _capturer.HandleInput(keyboardState, mouseState, frameTime);
            return;
        }

        if (keyboardState.IsKeyDown(Keys.Escape))
            Environment.Exit(0);

        foreach (var inputHandler in _inputHandlers)
        {
            if (inputHandler.HandleInput(keyboardState, mouseState, frameTime))
            {
                return;
            }
        }
    }
}