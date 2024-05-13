using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Buffers;

public class VertexBuffer
{
    private readonly int _handler;
    public int Length;

    private int _size;
    public int Size
    {
        get => _size;
        set
        {
            _size = value;
            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, Size, IntPtr.Zero, _usage);
        }
    }

    private readonly BufferUsageHint _usage;

    public VertexBuffer(int size = 10000, BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        _size = size;
        _usage = usage;

        _handler = GL.GenBuffer();
        Bind();
        GL.BufferData(BufferTarget.ArrayBuffer, _size, IntPtr.Zero, _usage);
    }

    public VertexBuffer(Vertex[] vertices, BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        _usage = usage;
        _handler = GL.GenBuffer();
        Bind();

        var coords = new List<float>();
        for (var i = 0; i < vertices.Length; i++)
        {
            // THIS IS UGLY

            // vertex
            coords.Add(vertices[i].Coordinates.X);
            coords.Add(vertices[i].Coordinates.Y);
            coords.Add(vertices[i].Coordinates.Z);

            coords.Add(vertices[i].UV.X);
            coords.Add(vertices[i].UV.Y);

            coords.Add(vertices[i].Normal.X);
            coords.Add(vertices[i].Normal.Y);
            coords.Add(vertices[i].Normal.Z);
        }

        Size = coords.Count * sizeof(float);
        GL.BufferData(BufferTarget.ArrayBuffer, Size, coords.ToArray(), _usage);
        Length = vertices.Length;
    }

    public void Bind()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, _handler);
    }

    public void Unload()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.DeleteBuffer(_handler);
    }
}