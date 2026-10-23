using Jellyfish.Console;
using Jellyfish.Debug;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;

namespace Jellyfish.Input;

public class InputManager
{
    private readonly List<IInputHandler> _inputHandlers = new();
    private bool _inputCaptured;
    private IInputHandler? _capturer;

    private readonly List<char> _pressedChars = new();

    public bool IsControllingCursor { get; set; }

    public void RegisterInputHandler(IInputHandler inputHandler)
    {
         _inputHandlers.Add(inputHandler);
    }

    public void UnregisterInputHandler(IInputHandler inputHandler)
    {
         _inputHandlers.Remove(inputHandler);
    }

    public void CaptureInput(IInputHandler inputHandler)
    {
        _inputCaptured = true;
        _capturer = inputHandler;
    }

    public void ReleaseInput(IInputHandler inputHandler)
    {
        if (inputHandler == _capturer)
        {
             _inputCaptured = false;
             _capturer = null;
        }
    }

    public void Frame(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        using var _ = new PerformanceMeasure("InputManager.Frame");

        if (_inputCaptured)
        {
            _capturer?.HandleInput(keyboardState, mouseState, frameTime);
            foreach (var pressedChar in _pressedChars)
            {
                _capturer?.HandleText(pressedChar);
            }
        }
        else
        {
            var handled = false;

            foreach (var pressedChar in _pressedChars)
            {
                foreach (var inputHandler in _inputHandlers)
                {
                    if (inputHandler.HandleText(pressedChar))
                    {
                        handled = true;
                        break;
                    }
                }

                if (handled)
                    break;
            }

            if (!handled)
            {
                foreach (var inputHandler in _inputHandlers)
                {
                    if (inputHandler.HandleInput(keyboardState, mouseState, frameTime))
                    {
                        break;
                    }
                }
            }
        }

        _pressedChars.Clear();
    }

    public void HandleTextInput(char input)
    {
        _pressedChars.Add(input);
    }
}