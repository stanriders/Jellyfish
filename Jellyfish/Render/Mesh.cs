using System.Collections.Generic;
using Jellyfish.Console;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders.Deferred;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

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

public struct Vertex
{
    public Vector3 Coordinates { get; set; }
    public Vector2 UV { get; set; }
    public Vector3 Normal { get; set; }
    public List<BoneLink> BoneLinks { get; set; } = new();

    public Vertex()
    {
    }

    public override string ToString()
    {
        return Coordinates.ToString();
    }
}

public class Mesh
{
    private IndexBuffer? _ibo;
    
    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;
    public bool ShouldDraw { get; set; } = true;
    public bool IsDev { get; set; }
    public Material? Material { get; set; }

    private Shader _shader = null!;
    private GeometryPass _gBufferShader = null!;
    private VertexArray _vao = null!;
    private VertexBuffer _vbo = null!;

    public Mesh(MeshPart mesh)
    {
        MeshPart = mesh;
        if (mesh.Texture != null)
        {
            AddMaterial(mesh.Texture);
        }
        else
        {
            Log.Context(this).Warning("Mesh {Name} doesn't have a texture!", mesh.Name);
            AddMaterial("materials/error.mat");
        }
        CreateBuffers();

        _vao.Unbind();
    }

    public MeshPart MeshPart { get; private set; }

    public virtual PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;

    protected void AddMaterial(string path)
    {
        Material = new Material(path);
        _shader = Material.GetShaderInstance();
        _gBufferShader = new GeometryPass(Material.Diffuse, Material.Normal);
    }

    protected void CreateBuffers()
    {
        _vbo = new VertexBuffer(MeshPart.Vertices.ToArray());

        if (MeshPart.Indices != null && MeshPart.Indices.Count > 0)
            _ibo = new IndexBuffer(MeshPart.Indices.ToArray());

        _vao = new VertexArray(_vbo, _ibo);

        var vertexLocation = _shader.GetAttribLocation("aPosition");
        GL.EnableVertexArrayAttrib(_vao.Handle, vertexLocation);
        GL.VertexArrayAttribFormat(_vao.Handle, vertexLocation, 3, VertexAttribType.Float, false, 0);

        var texCoordLocation = _shader.GetAttribLocation("aTexCoord");
        GL.EnableVertexArrayAttrib(_vao.Handle, texCoordLocation);
        GL.VertexArrayAttribFormat(_vao.Handle, texCoordLocation, 2, VertexAttribType.Float, false, 3 * sizeof(float));

        var normalLocation = _shader.GetAttribLocation("aNormal");
        GL.EnableVertexArrayAttrib(_vao.Handle, normalLocation);
        GL.VertexArrayAttribFormat(_vao.Handle, normalLocation, 3, VertexAttribType.Float, false, 5 * sizeof(float));

        GL.VertexArrayAttribBinding(_vao.Handle, vertexLocation, 0);
        GL.VertexArrayAttribBinding(_vao.Handle, texCoordLocation, 0);
        GL.VertexArrayAttribBinding(_vao.Handle, normalLocation, 0);
    }

    public void DrawGBuffer()
    {
        Draw(_gBufferShader);
    }

    public void Draw(Shader? shaderToUse = null)
    {
        var drawShader = shaderToUse ?? _shader;

        drawShader.Bind();
        _vao.Bind();

        var transform = Matrix4.Identity * Matrix4.CreateScale(Scale) * Matrix4.CreateTranslation(Position);
        drawShader.SetMatrix4("transform", transform);

        var rotation = Matrix4.Identity * Matrix4.CreateFromQuaternion(Rotation);
        drawShader.SetMatrix4("rotation", rotation);

        if (_ibo != null)
            GL.DrawElements(PrimitiveType, MeshPart.Indices!.Count, DrawElementsType.UnsignedInt, 0);
        else
            GL.DrawArrays(PrimitiveType, 0, _vbo.Length);

        drawShader.Unbind();
        _vao.Unbind();
    }

    public void SetMeshPart(MeshPart part)
    {
        _vbo.UpdateData(part.Vertices.ToArray());
        MeshPart = part;
    }

    public void Unload()
    {
        _vbo.Unload();
        _ibo?.Unload();
        _vao.Unload();
        _shader.Unload();
    }
}