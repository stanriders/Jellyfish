using System.Collections.Generic;
using System.IO;
using Jellyfish.Console;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders.Deferred;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public struct Bone
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? Parent { get; set; } = null;

    public override string ToString() => $"{Id} - {Name}";

    public Bone()
    {
    }
}

public struct BoneLink
{
    public int Id {get; set; }
    public float Weigth { get; set; }

    public override string ToString() => $"{Id} - {Weigth}";
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

    public override string ToString() => Coordinates.ToString();
}

public class Mesh
{
    private IndexBuffer? _ibo;
    private VertexArray _vao = null!;
    private VertexBuffer _vbo = null!;

    private Shader _shader = null!;
    private GeometryPass _gBufferShader = null!;

    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;

    public string Name { get; set; }
    public bool ShouldDraw { get; set; } = true;
    public bool IsDev { get; set; }
    public Material? Material { get; private set; }
    public List<Vertex> Vertices { get; private set; }
    public List<Bone> Bones { get; private set; }
    public List<uint>? Indices { get; private set; }
    public override string ToString() => Name;

    private BoundingBox? _boundingBox;
    public BoundingBox BoundingBox
    {
        get
        {
            _boundingBox ??= new BoundingBox(Vertices.ToArray());

            return _boundingBox.Value.Translate(Matrix4.Identity * Matrix4.CreateScale(Scale) *
                                                Matrix4.CreateFromQuaternion(Rotation));
        }
    }

    public Mesh(string name, List<Vertex>? vertices = null, List<uint>? indices = null, List<Bone>? bones = null, string? texture = null)
    {
        Name = name;
        Vertices = vertices ?? new List<Vertex>();
        Indices = indices;
        Bones = bones ?? new List<Bone>();

        // TODO: this should be handled by the material itself
        if (texture != null)
        {
            var modelFolder = $"materials/models/{name}";

            var matPath = $"{modelFolder}/{Path.GetFileNameWithoutExtension(texture)}.mat";
            if (!File.Exists(matPath))
            {
                matPath = $"{modelFolder}/{Path.GetFileName(texture)}";
                if (!File.Exists(matPath))
                {
                    matPath = texture;
                }
            }

            texture = matPath;
        }
        else
        {
            Log.Context(this).Warning("Mesh {Name} has no texture data!!", name);

            var modelFolder = $"materials/models/{name}";

            var matPath = $"{modelFolder}/{name}.mat";
            if (!File.Exists(matPath))
                matPath = null;

            texture = matPath;
        }

        if (texture != null)
        {
            AddMaterial(texture);
        }
        else
        {
            Log.Context(this).Warning("Mesh {Name} doesn't have a texture!", name);
            AddMaterial("materials/error.mat");
        }
    }

    public void Load()
    {
        CreateBuffers();
    }

    public virtual PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;
    public VertexBufferObjectUsage Usage { get; set; } = VertexBufferObjectUsage.StaticDraw;

    protected void AddMaterial(string path)
    {
        Material = new Material(path);

        _shader = Material.GetShaderInstance();
        _gBufferShader = new GeometryPass(Material.Diffuse, Material.Normal);
    }

    protected void CreateBuffers()
    {
        _vbo = new VertexBuffer(Vertices.ToArray(), Usage);

        if (Indices != null && Indices.Count > 0)
            _ibo = new IndexBuffer(Indices.ToArray());

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

        var boneIdsLocation = _shader.GetAttribLocation("aBoneIDs");
        GL.EnableVertexArrayAttrib(_vao.Handle, boneIdsLocation);
        GL.VertexArrayAttribFormat(_vao.Handle, boneIdsLocation, 4, VertexAttribType.Float, false, 8 * sizeof(float));

        var weightsLocation = _shader.GetAttribLocation("aWeights");
        GL.EnableVertexArrayAttrib(_vao.Handle, weightsLocation);
        GL.VertexArrayAttribFormat(_vao.Handle, weightsLocation, 4, VertexAttribType.Float, false, 12 * sizeof(float));

        GL.VertexArrayAttribBinding(_vao.Handle, vertexLocation, 0);
        GL.VertexArrayAttribBinding(_vao.Handle, texCoordLocation, 0);
        GL.VertexArrayAttribBinding(_vao.Handle, normalLocation, 0);
        GL.VertexArrayAttribBinding(_vao.Handle, boneIdsLocation, 0);
        GL.VertexArrayAttribBinding(_vao.Handle, weightsLocation, 0);

        _vao.Unbind();
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

        drawShader.SetInt("boneCount", Bones.Count);

        for (var i = 0; i < Bones.Count; i++)
        {
            drawShader.SetMatrix4($"bones[{i}]", Matrix4.Identity);
        }

        if (_ibo != null)
            GL.DrawElements(PrimitiveType, Indices!.Count, DrawElementsType.UnsignedInt, 0);
        else
            GL.DrawArrays(PrimitiveType, 0, _vbo.Length);

        drawShader.Unbind();
        _vao.Unbind();
    }

    public void Update(List<Vertex> vertices, List<uint>? indices = null)
    {
        _vbo.UpdateData(vertices.ToArray());
        Vertices = vertices;
        if (indices != null) 
        { 
            //_ibo.UpdateData(indices.ToArray());
            Indices = indices;
        }
    }

    public Matrix4 GetTransformationMatrix() => Matrix4.Identity * 
                                                Matrix4.CreateScale(Scale) *
                                                Matrix4.CreateTranslation(Position) *
                                                Matrix4.CreateFromQuaternion(Rotation);

    public void Unload()
    {
        _vbo.Unload();
        _ibo?.Unload();
        _vao.Unload();
        _shader.Unload();
        _gBufferShader.Unload();
    }
}