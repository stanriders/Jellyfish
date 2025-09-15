using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders.Deferred;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Jellyfish.Render;


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
    private readonly string? _texture;
    private IndexBuffer? _ibo;
    private VertexArray _vao = null!;
    private VertexBuffer _vbo = null!;

    private Shader? _shader;
    private GeometryPass _gBufferShader = null!;

    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;

    public string Name { get; set; }
    public bool ShouldDraw { get; set; } = true;
    public bool IsDev { get; set; }
    public Model? Model { get; set; }
    public Material? Material { get; private set; }
    public List<Vertex> Vertices { get; private set; }
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

    public Mesh(string name, List<Vertex>? vertices = null, List<uint>? indices = null, string? texture = null, Model? model = null)
    {
        _texture = texture;
        Model = model;
        Name = name;
        Vertices = vertices ?? [];
        Indices = indices;
    }

    public void Load()
    {
        AddMaterial(_texture);
        CreateBuffers();
    }

    public virtual PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;
    public BufferUsage Usage { get; set; } = BufferUsage.StaticDraw;

    protected void AddMaterial(string? path)
    {
        Material = new Material(path, Model != null ? Model.Name : Name);
        _shader = Material.Shader;
        _gBufferShader = new GeometryPass(Material);
    }

    protected void CreateBuffers()
    {
        _vbo = new VertexBuffer(Name, Vertices.ToArray(), Usage);

        if (Indices != null && Indices.Count > 0)
            _ibo = new IndexBuffer(Indices.ToArray());

        _vao = new VertexArray(_vbo, _ibo);

        var vertexLocation = _shader?.GetAttribLocation("aPosition");
        if (vertexLocation != null)
        {
            GL.EnableVertexArrayAttrib(_vao.Handle, vertexLocation.Value);
            GL.VertexArrayAttribFormat(_vao.Handle, vertexLocation.Value, 3, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(_vao.Handle, vertexLocation.Value, 0);
        }

        var texCoordLocation = _shader?.GetAttribLocation("aTexCoord");
        if (texCoordLocation != null)
        {
            GL.EnableVertexArrayAttrib(_vao.Handle, texCoordLocation.Value);
            GL.VertexArrayAttribFormat(_vao.Handle, texCoordLocation.Value, 2, VertexAttribType.Float, false, 3 * sizeof(float));
            GL.VertexArrayAttribBinding(_vao.Handle, texCoordLocation.Value, 0);
        }

        var normalLocation = _shader?.GetAttribLocation("aNormal");
        if (normalLocation != null)
        {
            GL.EnableVertexArrayAttrib(_vao.Handle, normalLocation.Value);
            GL.VertexArrayAttribFormat(_vao.Handle, normalLocation.Value, 3, VertexAttribType.Float, false, 5 * sizeof(float));
            GL.VertexArrayAttribBinding(_vao.Handle, normalLocation.Value, 0);
        }

        var boneIdsLocation = _shader?.GetAttribLocation("aBoneIDs");
        if (boneIdsLocation != null)
        {
            GL.EnableVertexArrayAttrib(_vao.Handle, boneIdsLocation.Value);
            GL.VertexArrayAttribFormat(_vao.Handle, boneIdsLocation.Value, 4, VertexAttribType.Float, false, 8 * sizeof(float));
            GL.VertexArrayAttribBinding(_vao.Handle, boneIdsLocation.Value, 0);
        }

        var weightsLocation = _shader?.GetAttribLocation("aWeights");
        if (weightsLocation != null)
        {
            GL.EnableVertexArrayAttrib(_vao.Handle, weightsLocation.Value);
            GL.VertexArrayAttribFormat(_vao.Handle, weightsLocation.Value, 4, VertexAttribType.Float, false, 12 * sizeof(float));
            GL.VertexArrayAttribBinding(_vao.Handle, weightsLocation.Value, 0);
        }

        _vao.Unbind();
    }

    public void DrawGBuffer()
    {
        Draw(_gBufferShader);
    }

    public void Draw(Shader? shaderToUse = null)
    {
        var drawShader = shaderToUse ?? _shader;
        if (drawShader == null)
            return;

        drawShader.Bind();
        _vao.Bind();

        var transform = Matrix4.Identity * Matrix4.CreateScale(Scale) * Matrix4.CreateTranslation(Position);
        drawShader.SetMatrix4("transform", transform);

        var rotation = Matrix4.Identity * Matrix4.CreateFromQuaternion(Rotation);
        drawShader.SetMatrix4("rotation", rotation);

        if (Model != null)
        {
            var boneMatrices = Model.BoneMatrices;

            drawShader.SetInt("boneCount", boneMatrices.Length);
            for (var i = 0; i < boneMatrices.Length; i++)
            {
                drawShader.SetMatrix4($"bones[{i}]", boneMatrices[i]);
            }
        }

        if (_ibo != null)
            GL.DrawElements(PrimitiveType, Indices!.Count, DrawElementsType.UnsignedInt, 0);
        else
            GL.DrawArrays(PrimitiveType, 0, _vbo.Length);

        PerformanceMeasurment.Increment("DrawCalls");

        drawShader.Unbind();
        _vao.Unbind();
    }

    public void Update(List<Vertex> vertices, List<uint>? indices = null)
    {
        _vbo.UpdateData(vertices.ToArray());
        Vertices = vertices;
        if (indices != null) 
        { 
            _ibo?.UpdateData(indices.ToArray());
            Indices = indices;
        }

        _boundingBox = null;
    }

    public void UpdateMaterial(string path)
    {
        _gBufferShader.Unload();
        Material?.Unload();

        AddMaterial(path);
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
        _gBufferShader.Unload();

        Material?.Unload();
    }
}