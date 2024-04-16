using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class IndexBuffer
{
    private readonly int _handler;

    public IndexBuffer(uint[] indices)
    {
        _handler = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _handler);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices,
            BufferUsageHint.StaticDraw);
    }

    public void Bind()
    {
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _handler);
    }

    public void Unload()
    {
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
        GL.DeleteBuffer(_handler);
    }
}