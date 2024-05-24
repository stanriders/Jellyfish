using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class VertexBuffer
{
    public readonly int Handle;
    public int Length;
    public int Stride;

    private int _size;
    public int Size
    {
        get => _size;
        set
        {
            _size = value;
            GL.NamedBufferData(Handle, Size, IntPtr.Zero, _usage);
        }
    }

    private readonly BufferUsageHint _usage;

    public VertexBuffer(int size = 10000, BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        _size = size;
        _usage = usage;

        GL.CreateBuffers(1, out Handle);
        GL.NamedBufferData(Handle, _size, IntPtr.Zero, _usage);
    }

    public VertexBuffer(Vertex[] vertices, BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        _usage = usage;

        GL.CreateBuffers(1, out Handle);

        var coords = new List<float>();
        foreach (var vertex in vertices)
        {
            // THIS IS UGLY

            // vertex
            coords.Add(vertex.Coordinates.X);
            coords.Add(vertex.Coordinates.Y);
            coords.Add(vertex.Coordinates.Z);

            coords.Add(vertex.UV.X);
            coords.Add(vertex.UV.Y);

            coords.Add(vertex.Normal.X);
            coords.Add(vertex.Normal.Y);
            coords.Add(vertex.Normal.Z);
        }

        Stride = 8 * sizeof(float);
        Size = coords.Count * sizeof(float);
        GL.NamedBufferData(Handle, Size, coords.ToArray(), _usage);
        Length = vertices.Length;
    }
    
    public void Unload()
    {
        GL.DeleteBuffer(Handle);
    }
}