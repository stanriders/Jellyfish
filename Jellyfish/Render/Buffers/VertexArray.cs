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

    public void Unbind()
    {
        GL.BindVertexArray(0);
    }

    public void Unload()
    {
        Unbind();
        GL.DeleteVertexArray(_vaoHandler);
    }
}