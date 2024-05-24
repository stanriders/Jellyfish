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

    private readonly BufferUsageHint _usage;

    public IndexBuffer(int size = 2000, BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        _usage = usage;
        _size = size;

        GL.CreateBuffers(1, out Handle);
        GL.NamedBufferData(Handle, _size, IntPtr.Zero, _usage);
    }

    public IndexBuffer(uint[] indices)
    {
        _usage = BufferUsageHint.StaticDraw;
        _size = indices.Length * sizeof(uint);

        GL.CreateBuffers(1, out Handle);
        GL.NamedBufferData(Handle, _size, indices, _usage);
    }
    
    public void Unload()
    {
        GL.DeleteBuffer(Handle);
    }
}