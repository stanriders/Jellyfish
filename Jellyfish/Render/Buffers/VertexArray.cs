using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class VertexArray
{
    private readonly int _vaoHandler;

    public VertexArray()
    {
        _vaoHandler = GL.GenVertexArray();
        GL.BindVertexArray(_vaoHandler);
    }

    public void Bind()
    {
        GL.BindVertexArray(_vaoHandler);
    }

    public void Unload()
    {
        GL.BindVertexArray(0);
        GL.DeleteVertexArray(_vaoHandler);
    }
}