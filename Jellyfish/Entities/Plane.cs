using System.Collections.Generic;
using System.IO;
using Jellyfish.Render;
using JoltPhysicsSharp;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("plane_flat")]
public class Plane : BaseModelEntity, IPhysicsEntity
{
    private BodyID? _physicsBodyId;

    public Plane()
    {
        AddProperty("Size", new Vector2(20, 20), changeCallback: OnSizeChanged);
        AddProperty("Texture", "test.png", changeCallback: OnTextureChanged);
        AddProperty("TextureScale", new Vector2(1.0f), changeCallback: OnSizeChanged);
    }

    private void OnTextureChanged(string path)
    {
        Model?.Meshes[0].UpdateMaterial(path);
    }

    private void OnSizeChanged(Vector2 obj)
    {
        Model?.Meshes[0].Update(GenerateVertices());

        if (_physicsBodyId != null)
        {
            PhysicsManager.RemoveObject(_physicsBodyId.Value);
            _physicsBodyId = PhysicsManager.AddStaticObject([Model!.Meshes[0]], this) ?? 0;
        }
    }

    public override void Load()
    {
        var mesh = GenerateMesh();
        if (mesh == null)
            return;

        Model = new Model(mesh)
        {
            Position = GetPropertyValue<Vector3>("Position"),
            Rotation = GetPropertyValue<Quaternion>("Rotation")
        };

        _physicsBodyId = PhysicsManager.AddStaticObject([mesh], this) ?? 0;
        base.Load();
    }

    public override void Unload()
    {
        if (_physicsBodyId != null)
            PhysicsManager.RemoveObject(_physicsBodyId.Value);

        base.Unload();
    }

    public void ResetVelocity()
    {
    }

    public void OnPhysicsPositionChanged(Vector3 position)
    {
    }

    public void OnPhysicsRotationChanged(Quaternion rotation)
    {
    }

    private Mesh? GenerateMesh()
    {
        var textureProperty = GetPropertyValue<string>("Texture");
        if (textureProperty == null)
        {
            EntityLog().Error("Texture not set!");
            return null;
        }

        return new Mesh("plane_flat", GenerateVertices(), texture: $"materials/{textureProperty}");
    }

    private List<Vertex> GenerateVertices()
    {
        var size = GetPropertyValue<Vector2>("Size");

        var a = new Vector3(-size.X / 2.0f, size.Y / 2.0f, 0);
        var b = new Vector3(size.X / 2.0f, size.Y / 2.0f, 0);
        var c = new Vector3(size.X / 2.0f, -size.Y / 2.0f, 0);
        var d = new Vector3(-size.X / 2.0f, -size.Y / 2.0f, 0);

        Vector3 u = b - a;
        Vector3 v = c - b;
        Vector3 normal = Vector3.Cross(u, v).Normalized();

        var textureScale = GetPropertyValue<Vector2>("TextureScale");

        return
        [
            new()
            {
                Coordinates = a,
                Normal = normal,
                UV = new(0, 0)
            },
            new()
            {
                Coordinates = b,
                Normal = normal,
                UV = new(textureScale.X, 0)
            },
            new()
            {
                Coordinates = c,
                Normal = normal,
                UV = new(textureScale.X, textureScale.Y)
            },
            new()
            {
                Coordinates = d,
                Normal = normal,
                UV = new(0, textureScale.Y)
            },
            new()
            {
                Coordinates = a,
                Normal = normal,
                UV = new(0, 0)
            },
            new()
            {
                Coordinates = c,
                Normal = normal,
                UV = new(textureScale.X, textureScale.Y)
            }
        ];
    }
}