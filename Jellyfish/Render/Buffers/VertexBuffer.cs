using System;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class VertexBuffer
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

    public VertexBuffer(string name, int size = 10000, BufferUsage usage = BufferUsage.StaticDraw)
    {
        _size = size;
        _usage = usage;

        GL.CreateBuffer(out Handle);
        GL.ObjectLabel(ObjectIdentifier.Buffer, (uint)Handle, name.Length, name);
        GL.NamedBufferData(Handle, _size, IntPtr.Zero, _usage);
    }

    public VertexBuffer(string name, float[] data, BufferUsage usage = BufferUsage.StaticDraw)
    {
        _size = data.Length * sizeof(float);
        _usage = usage;

        GL.CreateBuffer(out Handle);
        GL.ObjectLabel(ObjectIdentifier.Buffer, (uint)Handle, name.Length, name);
        GL.NamedBufferData(Handle, _size, data, _usage);
    }

    public void UpdateData(float[] data, BufferUsage? usage = null)
    {
        if (usage != null)
        {
            _usage = usage.Value;
        }

        _size = data.Length * sizeof(float);
        GL.NamedBufferData(Handle, _size, data, _usage);
    }

    public void Unload()
    {
        GL.DeleteBuffer(Handle);
    }
}