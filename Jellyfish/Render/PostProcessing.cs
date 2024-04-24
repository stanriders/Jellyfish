using Jellyfish.Input;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Render;

public class PostProcessing : IInputHandler
{
    private readonly Shaders.PostProcessing _shader;
    private readonly VertexArray _vertexArray;

    private bool _isEnabled = true;

    private readonly float[] _quad = {
        // positions   // texCoords
        -1.0f,  1.0f,  0.0f, 1.0f,
        -1.0f, -1.0f,  0.0f, 0.0f,
         1.0f, -1.0f,  1.0f, 0.0f,

        -1.0f,  1.0f,  0.0f, 1.0f,
         1.0f, -1.0f,  1.0f, 0.0f,
         1.0f,  1.0f,  1.0f, 1.0f
    };

    public PostProcessing(int colorHandle, int depthHandle)
    {
        var vertexBuffer = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _quad.Length * sizeof(float), _quad, BufferUsageHint.StaticDraw);

        _vertexArray = new VertexArray();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);

        _shader = new Shaders.PostProcessing(colorHandle, depthHandle);
        _shader.Bind();

        InputManager.RegisterInputHandler(this);
    }

    public void Draw()
    {
        GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);

        _shader.Bind();
        _shader.SetInt("isEnabled", _isEnabled ? 1 : 0);

        _vertexArray.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (keyboardState.IsKeyPressed(Keys.P))
        {
            _isEnabled = !_isEnabled;
            return true;
        }

        return false;
    }
}