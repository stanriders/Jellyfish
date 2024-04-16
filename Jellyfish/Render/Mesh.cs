using System.Collections.Generic;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public class MeshInfo
{
    public required string Name { get; set; }
    public string Texture { get; set; }
    public List<Vector3> Vertices { get; set; } = new();
    public List<Vector2> UVs { get; set; } = new();
    public List<Vector3> Normals { get; set; } = new();
    public List<uint> Indices { get; set; } // can be null
}

public class Mesh
{
    protected IndexBuffer ibo;

    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;

    protected Shader shader;
    protected VertexArray vao;
    protected VertexBuffer vbo;

    public Mesh()
    {
    }

    public Mesh(MeshInfo mesh)
    {
        MeshInfo = mesh;
        CreateBuffers();
    }

    public MeshInfo MeshInfo { get; }

    public virtual PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;

    public void AddShader(Shader shader)
    {
        this.shader = shader;
        shader.Draw();
    }

    public void CreateBuffers()
    {
        vbo = new VertexBuffer(MeshInfo.Vertices.ToArray(), MeshInfo.UVs.ToArray(), MeshInfo.Normals.ToArray());

        if (MeshInfo.Indices != null && MeshInfo.Indices.Count > 0)
            ibo = new IndexBuffer(MeshInfo.Indices.ToArray());

        vao = new VertexArray();
        vbo.Bind();
        ibo?.Bind();
    }

    public void Draw()
    {
        shader.Draw();

        vao.Bind();


        var transform = Matrix4.Identity * Matrix4.CreateTranslation(Position);

        shader.SetMatrix4("transform", transform);

        var rotation = Matrix4.Identity *
                       Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X)) *
                       Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y)) *
                       Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));

        shader.SetMatrix4("rotation", rotation);


        if (ibo != null)
            GL.DrawElements(PrimitiveType, MeshInfo.Indices.Count, DrawElementsType.UnsignedInt, 0);
        else
            GL.DrawArrays(PrimitiveType, 0, vbo.Length);
    }

    public void Unload()
    {
        vbo.Unload();
        ibo?.Unload();
        vao.Unload();
        shader.Unload();
    }
}