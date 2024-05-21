using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Input;

public class InputManager
{
    private readonly List<IInputHandler> _inputHandlers = new();

    private static InputManager? instance;

    public InputManager()
    {
        instance = this;
    }

    public static void RegisterInputHandler(IInputHandler inputHandler)
    {
        instance?._inputHandlers.Add(inputHandler);
    }

    public void Frame(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
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