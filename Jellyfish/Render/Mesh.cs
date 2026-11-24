using Jellyfish.Console;
using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;

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
    public bool UsesShaderBuffers { get; private set; }
    public override string ToString() => Name;

    private BoundingBox? _boundingBox;
    public BoundingBox BoundingBox
    {
        get
        {
            _boundingBox ??= new BoundingBox(Vertices.ToArray());

            return _boundingBox.Value.Translate(Matrix4.CreateFromQuaternion(Rotation) * Matrix4.CreateScale(Scale));
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

    public MeshBuffers Load(MeshBuffers? sharedBuffers = null)
    {
        AddMaterial(_texture);

        if (sharedBuffers != null)
        {
            (_vbo, _ibo, _vao) = sharedBuffers;
            UsesShaderBuffers = true;
            return sharedBuffers;
        }

        CreateBuffers();
        return new MeshBuffers(_vbo, _ibo, _vao);
    }

    public virtual PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;
    public BufferUsage Usage { get; set; } = BufferUsage.StaticDraw;

    protected void AddMaterial(string? path)
    {
        Material = new Material(path, Model != null ? Model.Name : Name);
    }

    protected void CreateBuffers()
    {
        _vbo = new VertexBuffer(Name, VerticesToArray(), Usage);

        if (Indices != null && Indices.Count > 0)
            _ibo = new IndexBuffer(Indices.ToArray());

        _vao = new VertexArray(_vbo, _ibo, 16 * sizeof(float));

        // must match CommonVertex.vert
        _vao.AddAttribute(0, 3, 0);                  // aPosition
        _vao.AddAttribute(1, 2, 3 * sizeof(float));  // aTexCoord
        _vao.AddAttribute(2, 3, 5 * sizeof(float));  // aNormal
        _vao.AddAttribute(3, 4, 8 * sizeof(float));  // aBoneIDs
        _vao.AddAttribute(4, 4, 12 * sizeof(float)); // aWeights

        _vao.Unbind();
    }

    public void Draw(string? shaderToUse = null)
    {
        var drawShader = Engine.ShaderManager.GetShader(shaderToUse ?? Material?.GetParam<string>("Shader"));
        if (drawShader == null)
            return;

        drawShader.BindMaterial(Material);
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
            drawShader.SetMatrix4("bones", boneMatrices);
        }
        else
        {
            drawShader.SetInt("boneCount", 0);
        }

        if (_ibo != null)
            GL.DrawElements(PrimitiveType, Indices!.Count, DrawElementsType.UnsignedInt, 0);
        else
            GL.DrawArrays(PrimitiveType, 0, Vertices.Count);

        PerformanceMeasurement.Increment("DrawCalls");

        drawShader.Unbind();
    }

    public void Update(List<Vertex> vertices, List<uint>? indices = null)
    {
        if (UsesShaderBuffers)
        {
            Log.Context(this).Error("Can't update mesh {Name} since it's using shared buffers", Name);
            return;
        }

        Vertices = vertices;
        _vbo.UpdateData(VerticesToArray());
        if (indices != null) 
        { 
            _ibo?.UpdateData(indices.ToArray());
            Indices = indices;
        }

        _boundingBox = null;
    }

    public void UpdateMaterial(string path)
    {
        Material?.Unload();

        AddMaterial(path);
    }

    public Matrix4 GetTransformationMatrix() => Matrix4.Identity *
                                                Matrix4.CreateFromQuaternion(Rotation) *
                                                Matrix4.CreateScale(Scale) *
                                                Matrix4.CreateTranslation(Position);

    public void Unload(bool disposeBuffers = true)
    {
        if (disposeBuffers)
        {
            _vbo.Dispose();
            _ibo?.Dispose();
            _vao.Dispose();
        }

        Material?.Unload();
    }

    private float[] VerticesToArray()
    {
        var coords = new List<float>();
        foreach (var vertex in Vertices)
        {
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

        return coords.ToArray();
    }
}