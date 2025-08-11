using System;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class IndexBuffer
{
    public readonly int Handle;
    
    private int _size;
    public int Size
    {
        get => _size;
        set
        {
            _size = value;
            GL.NamedBufferData(Handle, _size, IntPtr.Zero, _usage);
        }
    }

    private BufferUsage _usage;

    public IndexBuffer(int size = 2000, BufferUsage usage = BufferUsage.StaticDraw)
    {
        _usage = usage;
        _size = size;

        GL.CreateBuffer(out Handle);
        GL.NamedBufferData(Handle, _size, IntPtr.Zero, _usage);
    }

    public IndexBuffer(uint[] indices, BufferUsage usage = BufferUsage.StaticDraw)
    {
        _usage = usage;
        _size = indices.Length * sizeof(uint);

        GL.CreateBuffer(out Handle);
        GL.NamedBufferData(Handle, _size, indices, _usage);
    }

    public void UpdateData(uint[] indices, BufferUsage usage = BufferUsage.StaticDraw)
    {
        _usage = usage;
        _size = indices.Length * sizeof(uint);

        GL.NamedBufferData(Handle, _size, indices, _usage);
    }

    public void Unload()
    {
        GL.DeleteBuffer(Handle);
    }
}