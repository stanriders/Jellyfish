using Jellyfish.Render;
using Jellyfish.Utils;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using System;
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
        AddProperty("Texture", "test.png", changeCallback: OnTextureChanged, flags: EntityPropertyFlags.FilePath);
        AddProperty("TextureScale", new Vector2(1.0f), changeCallback: OnTextureScaleChanged);
        AddProperty("Bevel", 0.0f, changeCallback: _ => Model?.Meshes[0].Update(GenerateVertices()));
        AddProperty("BevelSegments", 4, changeCallback: _ => Model?.Meshes[0].Update(GenerateVertices()));
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
            _physicsBodyId = Engine.PhysicsManager.AddStaticBox(GetPropertyValue<Vector3>("Size"), this) ?? 0;
        }
    }

    public override void Load()
    {
        var mesh = GenerateMesh();
        if (mesh == null)
            return;

        Model = new Model($"box_{GetPropertyValue<string>("Name")}", mesh, [])
        {
            Position = GetPropertyValue<Vector3>("Position"),
            Rotation = GetPropertyValue<Quaternion>("Rotation")
        };

        _physicsBodyId = Engine.PhysicsManager.AddStaticBox(GetPropertyValue<Vector3>("Size"), this) ?? 0;
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

    private Mesh? GenerateMesh()
    {
        var textureProperty = GetPropertyValue<string>("Texture");
        if (textureProperty == null)
        {
            EntityLog().Error("Texture not set!");
            return null;
        }

        return new Mesh($"box_{GetPropertyValue<string>("Name")}", GenerateVertices(), texture: $"materials/{textureProperty}");
    }

    private List<Vertex> GenerateVertices()
    {
        var size = GetPropertyValue<Vector3>("Size");
        var textureScale = GetPropertyValue<Vector2>("TextureScale");
        var segments = Math.Max(1, GetPropertyValue<int>("BevelSegments"));
        var radius = Math.Clamp(GetPropertyValue<float>("Bevel"), 0f, Math.Min(size.X, Math.Min(size.Y, size.Z)));
        var inner = size - new Vector3(radius);

        var reversedCube = CommonShapes.Cube.Reverse().ToArray();
        var vertices = new List<Vertex>();
        for (int i = 0; i < CommonShapes.Cube.Length; i+=6)
        {
            var plane = reversedCube.Skip(i).Take(6).Select(x => x * size).ToArray();
            var normal = MathUtils.CalculateNormal(plane[0], plane[1], plane[2]);

            var origin = plane[0];
            var uAxis = plane[1] - plane[0];
            var vAxis = plane[4] - plane[0];
            var uDir = uAxis.Normalized();
            var vDir = vAxis.Normalized();
            var center = normal * Math.Abs(Vector3.Dot(size, normal));

            var uSamples = GetBevelSamples(Math.Abs(Vector3.Dot(size, uDir)), radius, segments);
            var vSamples = GetBevelSamples(Math.Abs(Vector3.Dot(size, vDir)), radius, segments);

            var grid = new Vertex[uSamples.Count, vSamples.Count];
            for (var u = 0; u < uSamples.Count; u++)
            {
                for (var v = 0; v < vSamples.Count; v++)
                {
                    var flatPoint = center + uDir * uSamples[u] + vDir * vSamples[v];
                    var innerPoint = Vector3.Clamp(flatPoint, -inner, inner);
                    var vertexNormal = radius > 0 ? (flatPoint - innerPoint).Normalized() : normal;
                    var position = innerPoint + vertexNormal * radius;

                    grid[u, v] = new Vertex
                    {
                        Coordinates = position,
                        Normal = vertexNormal,
                        UV = new(Vector3.Dot(position - origin, uAxis) / uAxis.LengthSquared * textureScale.X,
                                 Vector3.Dot(position - origin, vAxis) / vAxis.LengthSquared * textureScale.Y)
                    };
                }
            }

            for (var u = 0; u < uSamples.Count - 1; u++)
            {
                for (var v = 0; v < vSamples.Count - 1; v++)
                {
                    vertices.AddRange([
                        grid[u, v], grid[u + 1, v], grid[u + 1, v + 1],
                        grid[u + 1, v + 1], grid[u, v + 1], grid[u, v]
                    ]);
                }
            }
        }
        return vertices;
    }

    public static List<float> GetBevelSamples(float halfSize, float radius, int segments)
    {
        if (radius <= 0)
            return [-halfSize, halfSize];

        var innerHalfSize = halfSize - radius;
        var samples = new List<float>();

        for (var k = segments; k >= 1; k--)
            samples.Add(-innerHalfSize - radius * MathF.Tan(MathF.PI / 4 * k / segments));

        samples.Add(-innerHalfSize);
        if (innerHalfSize > 0.0001f)
            samples.Add(innerHalfSize);

        for (var k = 1; k <= segments; k++)
            samples.Add(innerHalfSize + radius * MathF.Tan(MathF.PI / 4 * k / segments));

        return samples;
    }
}
