using Jellyfish.Render;
using Jellyfish.Utils;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Entities;

[Entity("box")]
public class Box : BaseModelEntity, IPhysicsEntity
{
    private BodyID? _physicsBodyId;
    public Box()
    {
        AddProperty("Size", new Vector3(20, 20, 20), changeCallback: OnSizeChanged);
        AddProperty("Texture", "test.png", changeCallback: OnTextureChanged);
        AddProperty("TextureScale", new Vector2(1.0f), changeCallback: OnTextureScaleChanged);
    }

    private void OnTextureScaleChanged(Vector2 obj)
    {
        Model?.Meshes[0].Update(GenerateVertices());
    }

    private void OnTextureChanged(string path)
    {
        Model?.Meshes[0].UpdateMaterial(path);
    }

    private void OnSizeChanged(Vector3 obj)
    {
        Model?.Meshes[0].Update(GenerateVertices());

        if (_physicsBodyId != null)
        {
            Engine.PhysicsManager.RemoveObject(_physicsBodyId.Value);
            _physicsBodyId = Engine.PhysicsManager.AddStaticObject([Model!.Meshes[0]], this) ?? 0;
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

        _physicsBodyId = Engine.PhysicsManager.AddStaticObject([mesh], this) ?? 0;
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

        return new Mesh("box", GenerateVertices(), texture: $"materials/{textureProperty}");
    }

    private List<Vertex> GenerateVertices()
    {
        var size = GetPropertyValue<Vector3>("Size");
        var textureScale = GetPropertyValue<Vector2>("TextureScale");

        var vertices = new List<Vertex>();
        for (int i = 0; i < CommonShapes.Cube.Length; i+=6)
        {
            var plane = CommonShapes.Cube.Reverse().Skip(i).Take(6).Select(x=> x * size).ToArray();

            Vector3 u = plane[1] - plane[0];
            Vector3 v = plane[2] - plane[1];
            Vector3 normal = Vector3.Cross(u, v).Normalized();

            vertices.AddRange([
                new Vertex
                {
                    Coordinates = plane[0],
                    Normal = normal,
                    UV = new(0, 0)
                },
                new Vertex
                {
                    Coordinates = plane[1],
                    Normal = normal,
                    UV = new(textureScale.X, 0)
                },
                new Vertex
                {
                    Coordinates = plane[2],
                    Normal = normal,
                    UV = new(textureScale.X, textureScale.Y)
                },
                new Vertex
                {
                    Coordinates = plane[3],
                    Normal = normal,
                    UV = new(textureScale.X, textureScale.Y)
                },
                new Vertex
                {
                    Coordinates = plane[4],
                    Normal = normal,
                    UV = new(0, textureScale.Y)
                },
                new Vertex
                {
                    Coordinates = plane[5],
                    Normal = normal,
                    UV = new(0,0)
                }
            ]);
        }
        return vertices;
    }
}
