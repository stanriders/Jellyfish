using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jellyfish.Debug;

namespace Jellyfish.Input;

public class InputManager
{
    private readonly List<IInputHandler> _inputHandlers = new();
    private bool _inputCaptured;
    private IInputHandler? _capturer;

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
        var stopwatch = Stopwatch.StartNew();
        if (_inputCaptured)
        {
            _capturer?.HandleInput(keyboardState, mouseState, frameTime);
        }
        else
        {
            foreach (var inputHandler in _inputHandlers.AsEnumerable().Reverse())
            {
                if (inputHandler.HandleInput(keyboardState, mouseState, frameTime))
                {
                    break;
                }
            }
        }

        PerformanceMeasurment.Add("InputManager.Frame", stopwatch.Elapsed.TotalMilliseconds);
    }
}