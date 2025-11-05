using Jellyfish.Debug;
using Jellyfish.Input;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using Jellyfish.Utils;

namespace Jellyfish.Render;

public class FinalOut : IInputHandler
{
    private readonly Shaders.PostProcessing _shader;

    public FinalOut()
    {
        _shader = new Shaders.PostProcessing();
        Engine.InputManager.RegisterInputHandler(this);
    }

    public void Draw()
    {
        var stopwatch = Stopwatch.StartNew();
        GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);

        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        _shader.Bind();
        CommonShapes.DrawQuad();
        _shader.Unbind();

        PerformanceMeasurment.Add("FinalOut.Draw", stopwatch.Elapsed.TotalMilliseconds);
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (keyboardState.IsKeyPressed(Keys.P))
        {
            _shader.IsEnabled = !_shader.IsEnabled;
            return true;
        }

        return false;
    }

    public void Unload()
    {
        _shader.Unload();
        Engine.InputManager.UnregisterInputHandler(this);
    }
}