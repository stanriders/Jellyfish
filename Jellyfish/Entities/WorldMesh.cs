using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfish.Render;
using JoltPhysicsSharp;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

// Editable meshes are treated as immutable
public record EditableFace
{
    public int[] Indices { get; init; } = [];
    public string Material { get; init; } = string.Empty;
    public Vector2 TextureOffset { get; init; }
    public Vector2 TextureScale { get; init; } = Vector2.One;
    public float TextureRotation { get; init; }
}

public record EditableMesh
{
    public Vector3[] Vertices { get; init; } = [];
    public EditableFace[] Faces { get; init; } = [];

    public override string ToString() => $"{Vertices.Length} vertices, {Faces.Length} faces";
}

[Entity("world_mesh")]
public class WorldMesh : BaseModelEntity, IPhysicsEntity
{
    private const float texture_world_size = 64.0f;

    private BodyID? _physicsBodyId;
    private EditableMesh? _builtMesh;
    private string[] _builtMaterials = [];

    public Matrix4 Transform => Matrix4.CreateFromQuaternion(GetPropertyValue<Quaternion>("Rotation")) *
                                Matrix4.CreateScale(GetPropertyValue<Vector3>("Scale")) *
                                Matrix4.CreateTranslation(GetPropertyValue<Vector3>("Position"));

    public WorldMesh()
    {
        AddProperty("Mesh", new EditableMesh(), changeCallback: _ =>
        {
            if (Loaded)
                UpdateMesh();
        });
    }

    public override void Load()
    {
        UpdateMesh();
        base.Load();
    }

    public override void Unload()
    {
        if (_physicsBodyId != null)
            Engine.PhysicsManager.RemoveObject(_physicsBodyId.Value);

        base.Unload();
    }

    public void ResetVelocity()
    {
    }

    public void OnPhysicsChanged(Vector3 position, Quaternion rotation)
    {
    }

    protected override void OnPositionChanged(Vector3 position)
    {
        base.OnPositionChanged(position);

        if (!Loaded)
            return;

        // texture coordinates are world aligned so they need to be regenerated
        UpdateVertices();

        if (_physicsBodyId != null)
            Engine.PhysicsManager.SetPosition(_physicsBodyId.Value, position);
    }

    protected override void OnRotationChanged(Quaternion rotation)
    {
        base.OnRotationChanged(rotation);

        if (!Loaded)
            return;

        UpdateVertices();

        if (_physicsBodyId != null)
            Engine.PhysicsManager.SetRotation(_physicsBodyId.Value, rotation);
    }

    protected override void OnScaleChanged(Vector3 scale)
    {
        base.OnScaleChanged(scale);

        if (!Loaded)
            return;

        UpdateVertices();
        UpdatePhysics();
    }

    private void UpdateMesh()
    {
        var mesh = GetPropertyValue<EditableMesh>("Mesh")!;
        var materials = mesh.Faces.Where(x => x.Indices.Length >= 3).Select(x => x.Material).Distinct().ToArray();
        var geometryChanged = _builtMesh == null || _builtMesh.Vertices != mesh.Vertices ||
                              !_builtMesh.Faces.Select(x => x.Indices).SequenceEqual(mesh.Faces.Select(x => x.Indices));

        _builtMesh = mesh;

        // only texture mapping has changed, existing meshes can be reused
        if (Model != null && !geometryChanged && materials.SequenceEqual(_builtMaterials))
        {
            UpdateVertices();
            return;
        }

        Model?.Unload();
        Model = null;
        _builtMaterials = materials;

        if (materials.Length > 0)
        {
            // one mesh per material since meshes can only have one material
            var name = $"world_mesh_{Name}";
            var transform = Transform;
            var meshes = materials.Select(x => new Mesh($"{name}_{x}", GenerateVertices(mesh, x, transform), texture: $"materials/{x}")).ToList();

            Model = new Model(name, meshes, [], [])
            {
                Position = GetPropertyValue<Vector3>("Position"),
                Rotation = GetPropertyValue<Quaternion>("Rotation"),
                Scale = GetPropertyValue<Vector3>("Scale")
            };
        }

        if (geometryChanged)
            UpdatePhysics();
    }

    private void UpdateVertices()
    {
        if (Model == null || _builtMesh == null)
            return;

        var transform = Transform;
        for (var i = 0; i < _builtMaterials.Length; i++)
            Model.Meshes[i].Update(GenerateVertices(_builtMesh, _builtMaterials[i], transform));
    }

    private void UpdatePhysics()
    {
        if (_physicsBodyId != null)
            Engine.PhysicsManager.RemoveObject(_physicsBodyId.Value);

        _physicsBodyId = Model != null ? Engine.PhysicsManager.AddStaticObject(Model.Meshes.ToArray(), this) : null;
    }

    private static List<Vertex> GenerateVertices(EditableMesh mesh, string material, Matrix4 transform)
    {
        var vertices = new List<Vertex>();

        foreach (var face in mesh.Faces.Where(x => x.Material == material && x.Indices.Length >= 3))
        {
            // Newell's method, works for any polygon even if some of the points are collinear
            var normal = Vector3.Zero;
            for (var i = 0; i < face.Indices.Length; i++)
                normal += Vector3.Cross(mesh.Vertices[face.Indices[i]], mesh.Vertices[face.Indices[(i + 1) % face.Indices.Length]]);

            normal.Normalize();

            // project texture coordinates from the world axis closest to the face normal
            var worldNormal = Vector3.TransformNormal(normal, transform);
            var absNormal = new Vector3(MathF.Abs(worldNormal.X), MathF.Abs(worldNormal.Y), MathF.Abs(worldNormal.Z));
            var (sin, cos) = MathF.SinCos(MathHelper.DegreesToRadians(face.TextureRotation));

            // triangle fan, faces are expected to be convex
            for (var i = 1; i < face.Indices.Length - 1; i++)
            {
                foreach (var index in new[] { face.Indices[0], face.Indices[i], face.Indices[i + 1] })
                {
                    var position = mesh.Vertices[index];
                    var worldPosition = Vector3.TransformPosition(position, transform);

                    Vector2 uv;
                    if (absNormal.X >= absNormal.Y && absNormal.X >= absNormal.Z)
                        uv = new Vector2(-MathF.Sign(worldNormal.X) * worldPosition.Z, -worldPosition.Y);
                    else if (absNormal.Y >= absNormal.Z)
                        uv = new Vector2(worldPosition.X, MathF.Sign(worldNormal.Y) * worldPosition.Z);
                    else
                        uv = new Vector2(MathF.Sign(worldNormal.Z) * worldPosition.X, -worldPosition.Y);

                    uv = new Vector2(uv.X * cos - uv.Y * sin, uv.X * sin + uv.Y * cos);

                    vertices.Add(new Vertex
                    {
                        Coordinates = position,
                        Normal = normal,
                        UV = uv / (texture_world_size * face.TextureScale) + face.TextureOffset
                    });
                }
            }
        }

        return vertices;
    }
}
