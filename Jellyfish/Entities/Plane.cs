using System.Collections.Generic;
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
        AddProperty("Size", new Vector2(20, 20), false);
        AddProperty("Texture", "test.png", false);
        AddProperty("TextureScale", new Vector2(1.0f), false);
    }

    public override void Load()
    {
        var size = GetPropertyValue<Vector2>("Size");
        var textureProperty = GetPropertyValue<string>("Texture");
        if (textureProperty == null)
        {
            EntityLog().Error("Texture not set!");
            return;
        }

        var a = new Vector3(-size.X / 2.0f, size.Y / 2.0f, 0);
        var b = new Vector3(size.X / 2.0f, size.Y / 2.0f, 0);
        var c = new Vector3(size.X / 2.0f, -size.Y / 2.0f, 0);
        var d = new Vector3(-size.X / 2.0f, -size.Y / 2.0f, 0);

        Vector3 u = b - a;
        Vector3 v = c - b;
        Vector3 normal = Vector3.Cross(u, v).Normalized();

        var texture = $"materials/{textureProperty}";
        var textureScale = GetPropertyValue<Vector2>("TextureScale");

        var meshPart = new MeshPart
        {
            Name = "plane_flat",
            Vertices = new List<Vertex>
            {
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
                },
            },
            Texture = texture
        };

        Model = new Model(new Mesh(meshPart))
        {
            Position = GetPropertyValue<Vector3>("Position"),
            Rotation = GetPropertyValue<Quaternion>("Rotation")
        };

        _physicsBodyId = PhysicsManager.AddStaticObject([meshPart], this) ?? 0;
        base.Load();
    }

    public override void Unload()
    {
        if (_physicsBodyId != null)
            PhysicsManager.RemoveObject(_physicsBodyId.Value);

        base.Unload();
    }

    public void OnPhysicsPositionChanged(Vector3 position)
    {
    }

    public void OnPhysicsRotationChanged(Quaternion rotation)
    {
    }
}