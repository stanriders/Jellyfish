using System;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class VertexArray : IDisposable
{
    public readonly int Handle;
    public readonly int Stride;

    public VertexArray(VertexBuffer vbo, IndexBuffer? ibo, int stride)
    {
        Stride = stride;
        GL.CreateVertexArray(out Handle);

        GL.VertexArrayVertexBuffer(Handle, 0, vbo.Handle, 0, Stride);
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

    public void AddAttribute(uint location, int size, uint offset,
        VertexAttribType type = VertexAttribType.Float, bool normalized = false)
    {
        GL.EnableVertexArrayAttrib(Handle, location);
        GL.VertexArrayAttribFormat(Handle, location, size, type, normalized, offset);
        GL.VertexArrayAttribBinding(Handle, location, 0);
    }

    public void Dispose()
    {
        Unbind();
        GL.DeleteVertexArray(Handle);
    }
}