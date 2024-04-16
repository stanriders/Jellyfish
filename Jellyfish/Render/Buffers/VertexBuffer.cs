using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Buffers;

public class VertexBuffer
{
    private readonly int _handler;
    public int Length;

    public VertexBuffer(Vector3[] vertices, Vector2[] uvs, Vector3[] normals,
        BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        _handler = GL.GenBuffer();
        Bind();

        var coords = new List<float>();
        for (var i = 0; i < vertices.Length; i++)
        {
            // THIS IS UGLY

            // vertex
            coords.Add(vertices[i].X);
            coords.Add(vertices[i].Y);
            coords.Add(vertices[i].Z);

            // uv
            if (uvs != null && uvs.Length > 0 && i < uvs.Length)
            {
                coords.Add(uvs[i].X);
                coords.Add(uvs[i].Y);
            }
            else
            {
                coords.Add(0.0f);
                coords.Add(0.0f);
            }

            // normal
            if (normals != null && normals.Length > 0 && i < normals.Length)
            {
                coords.Add(normals[i].X);
                coords.Add(normals[i].Y);
                coords.Add(normals[i].Z);
            }
            else
            {
                coords.Add(0.0f);
                coords.Add(0.0f);
                coords.Add(0.0f);
            }
        }

        GL.BufferData(BufferTarget.ArrayBuffer, coords.Count * sizeof(float), coords.ToArray(), usage);
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