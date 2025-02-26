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

    private readonly VertexBufferObjectUsage _usage;

    public IndexBuffer(int size = 2000, VertexBufferObjectUsage usage = VertexBufferObjectUsage.StaticDraw)
    {
        _usage = usage;
        _size = size;

        GL.CreateBuffer(out Handle);
        GL.NamedBufferData(Handle, _size, IntPtr.Zero, _usage);
    }

    public IndexBuffer(uint[] indices)
    {
        _usage = VertexBufferObjectUsage.StaticDraw;
        _size = indices.Length * sizeof(uint);

        GL.CreateBuffer(out Handle);
        GL.NamedBufferData(Handle, _size, indices, _usage);
    }
    
    public void Unload()
    {
        GL.DeleteBuffer(Handle);
    }
}