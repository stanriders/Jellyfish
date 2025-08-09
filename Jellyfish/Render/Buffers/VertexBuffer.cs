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

    public VertexBuffer(string name, float[] data, int stride, BufferUsage usage = BufferUsage.StaticDraw)
    {
        _size = data.Length * sizeof(float);
        _usage = usage;
        Stride = stride;

        GL.CreateBuffer(out Handle);
        GL.ObjectLabel(ObjectIdentifier.Buffer, (uint)Handle, name.Length, name);
        GL.NamedBufferData(Handle, _size, data, _usage);
    }

    public VertexBuffer(string name, Vertex[] vertices, BufferUsage usage = BufferUsage.StaticDraw)
    {
        _usage = usage;

        GL.CreateBuffer(out Handle);
        GL.ObjectLabel(ObjectIdentifier.Buffer, (uint)Handle, name.Length, name);

        var coords = new List<float>();
        foreach (var vertex in vertices)
        {
            // THIS IS SUPER UGLY

            // vertex
            coords.Add(vertex.Coordinates.X);
            coords.Add(vertex.Coordinates.Y);
            coords.Add(vertex.Coordinates.Z);

            coords.Add(vertex.UV.X);
            coords.Add(vertex.UV.Y);

            coords.Add(vertex.Normal.X);
            coords.Add(vertex.Normal.Y);
            coords.Add(vertex.Normal.Z);

            for (var i = 0; i < 4; i++)
            {
                if (vertex.BoneLinks.Count > i)
                    coords.Add(vertex.BoneLinks[i].Id);
                else
                    coords.Add(0);
            }

            for (var i = 0; i < 4; i++)
            {
                if (vertex.BoneLinks.Count > i)
                    coords.Add(vertex.BoneLinks[i].Weigth);
                else
                    coords.Add(0f);
            }
        }

        Stride = 16 * sizeof(float);
        _size = coords.Count * sizeof(float);
        GL.NamedBufferData(Handle, _size, coords.ToArray(), _usage);
        Length = vertices.Length;
    }

    public void UpdateData(Vertex[] vertices, BufferUsage? usage = null)
    {
        if (usage != null)
        {
            _usage = usage.Value;
        }

        var coords = new List<float>();
        foreach (var vertex in vertices)
        {
            // THIS IS SUPER UGLY

            // vertex
            coords.Add(vertex.Coordinates.X);
            coords.Add(vertex.Coordinates.Y);
            coords.Add(vertex.Coordinates.Z);

            coords.Add(vertex.UV.X);
            coords.Add(vertex.UV.Y);

            coords.Add(vertex.Normal.X);
            coords.Add(vertex.Normal.Y);
            coords.Add(vertex.Normal.Z);

            for (var i = 0; i < 4; i++)
            {
                if (vertex.BoneLinks?.Count > i)
                    coords.Add(vertex.BoneLinks[i].Id);
                else
                    coords.Add(0);
            }

            for (var i = 0; i < 4; i++)
            {
                if (vertex.BoneLinks?.Count > i)
                    coords.Add(vertex.BoneLinks[i].Weigth);
                else
                    coords.Add(0f);
            }
        }

        Stride = 16 * sizeof(float);
        _size = coords.Count * sizeof(float);
        GL.NamedBufferData(Handle, _size, coords.ToArray(), _usage);
        Length = vertices.Length;
    }

    public void Unload()
    {
        GL.DeleteBuffer(Handle);
    }
}