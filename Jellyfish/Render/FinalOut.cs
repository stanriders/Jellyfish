using Jellyfish.Debug;
using Jellyfish.Input;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Diagnostics;
using Jellyfish.Utils;

namespace Jellyfish.Render;

public class FinalOut : IInputHandler
{
    private readonly Shaders.PostProcessing _shader;
    private readonly VertexArray _vertexArray;
    private readonly VertexBuffer _vertexBuffer;

    public FinalOut()
    {
        _vertexBuffer = new VertexBuffer("FinalQuad", CommonShapes.Quad, 4 * sizeof(float));
        _vertexArray = new VertexArray(_vertexBuffer, null);

        _shader = new Shaders.PostProcessing();

        var vertexLocation = _shader.GetAttribLocation("aPos");
        if (vertexLocation != null)
        {
            GL.EnableVertexArrayAttrib(_vertexArray.Handle, vertexLocation.Value);
            GL.VertexArrayAttribFormat(_vertexArray.Handle, vertexLocation.Value, 2, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(_vertexArray.Handle, vertexLocation.Value, 0);
        }

        var texCoordLocation = _shader.GetAttribLocation("aTexCoords");
        if (texCoordLocation != null)
        {
            GL.EnableVertexArrayAttrib(_vertexArray.Handle, texCoordLocation.Value);
            GL.VertexArrayAttribFormat(_vertexArray.Handle, texCoordLocation.Value, 2, VertexAttribType.Float, false,
                2 * sizeof(float));
            GL.VertexArrayAttribBinding(_vertexArray.Handle, texCoordLocation.Value, 0);
        }

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
        _vertexArray.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        PerformanceMeasurment.Increment("DrawCalls");

        _vertexArray.Unbind();
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
        _vertexArray.Unload();
        _vertexBuffer.Unload();

        Engine.InputManager.UnregisterInputHandler(this);
    }
}