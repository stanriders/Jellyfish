using System;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class IndexBuffer
{
    private readonly int _handler;
    
    private int _size;
    public int Size
    {
        get => _size;
        set
        {
            _size = value;
            Bind();
            GL.BufferData(BufferTarget.ElementArrayBuffer, _size, IntPtr.Zero, _usage);
        }
    }

    private readonly BufferUsageHint _usage;

    public IndexBuffer(int size = 2000, BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        _usage = usage;
        _size = size;

        _handler = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _handler);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _size, IntPtr.Zero, _usage);
    }

    public IndexBuffer(uint[] indices)
    {
        _usage = BufferUsageHint.StaticDraw;
        _size = indices.Length * sizeof(uint);

        _handler = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _handler);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _size, indices, _usage);
    }

    public void Bind()
    {
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _handler);
    }

    public void Unbind()
    {
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    public void Unload()
    {
        Unbind();
        GL.DeleteBuffer(_handler);
    }
}