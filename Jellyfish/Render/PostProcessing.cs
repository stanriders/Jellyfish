using Jellyfish.Input;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Render;

public class PostProcessing : IInputHandler
{
    private readonly Shaders.PostProcessing _shader;
    private readonly VertexArray _vertexArray;

    private bool _isEnabled;

    private readonly float[] _quad = {
        // positions   // texCoords
        -1.0f,  1.0f,  0.0f, 1.0f,
        -1.0f, -1.0f,  0.0f, 0.0f,
         1.0f, -1.0f,  1.0f, 0.0f,

        -1.0f,  1.0f,  0.0f, 1.0f,
         1.0f, -1.0f,  1.0f, 0.0f,
         1.0f,  1.0f,  1.0f, 1.0f
    };

    public PostProcessing(RenderTarget color, RenderTarget depth)
    {
        var vertexBuffer = new VertexBuffer(_quad, 4 * sizeof(float));
        _vertexArray = new VertexArray(vertexBuffer, null);

        _shader = new Shaders.PostProcessing(color, depth);

        var vertexLocation = _shader.GetAttribLocation("aPos");
        GL.EnableVertexArrayAttrib(_vertexArray.Handle, vertexLocation);
        GL.VertexArrayAttribFormat(_vertexArray.Handle, vertexLocation, 2, VertexAttribType.Float, false, 0);

        var texCoordLocation = _shader.GetAttribLocation("aTexCoords");
        GL.EnableVertexArrayAttrib(_vertexArray.Handle, texCoordLocation);
        GL.VertexArrayAttribFormat(_vertexArray.Handle, texCoordLocation, 2, VertexAttribType.Float, false, 2 * sizeof(float));

        GL.VertexArrayAttribBinding(_vertexArray.Handle, vertexLocation, 0);
        GL.VertexArrayAttribBinding(_vertexArray.Handle, texCoordLocation, 0);

        InputManager.RegisterInputHandler(this);
    }

    public void Draw()
    {
        GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);

        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

        _shader.Bind();
        _shader.SetInt("isEnabled", _isEnabled ? 1 : 0);

        _vertexArray.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _vertexArray.Unbind();
        _shader.Unbind();
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