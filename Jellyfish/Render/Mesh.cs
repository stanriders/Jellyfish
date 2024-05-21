using System.Collections.Generic;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Render;

public class MeshPart
{
    public required string Name { get; set; }
    public string? Texture { get; set; }
    public List<Vertex> Vertices { get; set; } = new();
    public List<Bone> Bones { get; set; } = new();
    public List<uint>? Indices { get; set; } // can be null

    public override string ToString()
    {
        return Name;
    }
}

public struct Bone
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? Parent { get; set; } = null;

    public override string ToString()
    {
        return $"{Id} - {Name}";
    }

    public Bone()
    {
    }
}

public struct BoneLink
{
    public int Id {get; set; }
    public float Weigth { get; set; }

    public override string ToString()
    {
        return $"{Id} - {Weigth}";
    }
}

public class Vertex
{
    public Vector3 Coordinates { get; set; }
    public Vector2 UV { get; set; }
    public Vector3 Normal { get; set; }
    public List<BoneLink> BoneLinks { get; set; } = new();

    public override string ToString()
    {
        return Coordinates.ToString();
    }
}

public class Mesh
{
    protected IndexBuffer? ibo;
    
    public Vector3 Position = Vector3.Zero;
    public Vector3 Rotation = Vector3.Zero;

    protected Shader shader = null!;
    protected VertexArray vao = null!;
    protected VertexBuffer vbo = null!;

    public Mesh()
    {
    }

    public Mesh(MeshPart mesh)
    {
        MeshPart = mesh;
        CreateBuffers();
        if (mesh.Texture != null)
        {
            AddMaterial(mesh.Texture);
        }
        else
        {
            Log.Warning("Mesh {Name} doesn't have a texture!", mesh.Name);
            AddMaterial("materials/error.mat");
        }

        vao.Unbind();
    }

    public MeshPart MeshPart { get; set; } = null!;

    public virtual PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;

    protected void AddMaterial(string path)
    {
        var material = new Material(path);
        shader = material.GetShaderInstance();
        shader.Bind();
    }

    protected void CreateBuffers()
    {
        vbo = new VertexBuffer(MeshPart.Vertices.ToArray());

        if (MeshPart.Indices != null && MeshPart.Indices.Count > 0)
            ibo = new IndexBuffer(MeshPart.Indices.ToArray());

        vao = new VertexArray();
        vbo.Bind();
        ibo?.Bind();
    }

    public void Draw(Shader? shaderToUse = null)
    {
        var drawShader = shaderToUse ?? shader;

        drawShader.Bind();
        vao.Bind();

        var transform = Matrix4.Identity * Matrix4.CreateTranslation(Position);

        drawShader.SetMatrix4("transform", transform);

        var rotation = Matrix4.Identity *
                       Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X)) *
                       Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y)) *
                       Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z));

        drawShader.SetMatrix4("rotation", rotation);

        if (ibo != null)
            GL.DrawElements(PrimitiveType, MeshPart.Indices!.Count, DrawElementsType.UnsignedInt, 0);
        else
            GL.DrawArrays(PrimitiveType, 0, vbo.Length);

        drawShader.Unbind();
        vao.Unbind();
    }

    public void Unload()
    {
        vbo.Unload();
        ibo?.Unload();
        vao.Unload();
        shader.Unload();
    }
}