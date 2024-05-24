using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class VertexArray
{
    public readonly int Handle;

    public VertexArray(VertexBuffer vbo, IndexBuffer? ibo)
    {
        GL.CreateVertexArrays(1, out Handle);

        GL.VertexArrayVertexBuffer(Handle, 0, vbo.Handle, 0, vbo.Stride);
        if (ibo != null)
            GL.VertexArrayElementBuffer(Handle, ibo.Handle);
    }

    public void Bind()
    {
        GL.BindVertexArray(Handle);
    }

    public void Unbind()
    {
        GL.BindVertexArray(0);
    }

    public void Unload()
    {
        Unbind();
        GL.DeleteVertexArray(Handle);
    }
}