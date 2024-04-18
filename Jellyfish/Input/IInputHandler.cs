using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Input;

public interface IInputHandler
{
    bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime);
}