using Jellyfish.Debug;
using OpenTK.Graphics.OpenGL;
using System;

namespace Jellyfish.Render.Buffers;

public class VertexBuffer
{
    public readonly int Handle;

    private int _size;
    private NativeMemoryMeasurement.NativeMemoryTracker? _memoryTracker;

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

        _memoryTracker = NativeMemoryMeasurement.AddMemory(this, _size);
    }

    public VertexBuffer(string name, float[] data, BufferUsage usage = BufferUsage.StaticDraw)
    {
        _size = data.Length * sizeof(float);
        _usage = usage;

        GL.CreateBuffer(out Handle);
        GL.ObjectLabel(ObjectIdentifier.Buffer, (uint)Handle, name.Length, name);
        GL.NamedBufferData(Handle, _size, data, _usage);

        _memoryTracker = NativeMemoryMeasurement.AddMemory(this, _size);
    }

    public void UpdateData(float[] data, BufferUsage? usage = null)
    {
        if (usage != null)
        {
            _usage = usage.Value;
        }

        _size = data.Length * sizeof(float);
        GL.NamedBufferData(Handle, _size, data, _usage);

        _memoryTracker?.Dispose();
        _memoryTracker = NativeMemoryMeasurement.AddMemory(this, _size);
    }

    public void Unload()
    {
        GL.DeleteBuffer(Handle);
        _memoryTracker?.Dispose();
    }
}