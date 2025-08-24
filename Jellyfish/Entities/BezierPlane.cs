using Jellyfish.Render;
using System.Collections.Generic;
using OpenTK.Mathematics;
using JoltPhysicsSharp;

namespace Jellyfish.Entities;

[Entity("plane_bezier")]
public class BezierPlane : BaseModelEntity, IPhysicsEntity
{
    private BodyID? _physicsBodyId;

    public BezierPlane()
    {
        AddProperty("QuadSize", 20, changeCallback: _ => UpdateMesh());
        AddProperty("Texture", "test.png", changeCallback: OnTextureChanged);
        AddProperty("TextureScale", new Vector2(1.0f), changeCallback: _ => UpdateMesh());
        AddProperty("ControlPoints", GenerateInitialControlPoints(), showGizmo: true, changeCallback: _ => UpdateMesh());
    }

    private void OnTextureChanged(string path)
    {
        Model?.Meshes[0].UpdateMaterial(path);
    }

    private void UpdateMesh()
    {
        Model?.Meshes[0].Update(GenerateBezierPlane(), GenerateGridIndices());

        if (_physicsBodyId != null)
        {
            Engine.PhysicsManager.RemoveObject(_physicsBodyId.Value);
            _physicsBodyId = Engine.PhysicsManager.AddStaticObject([Model!.Meshes[0]], this) ?? 0;
        }
    }

    public override void Load()
    {
        var texture = GetPropertyValue<string>("Texture");
        if (texture == null)
        {
            EntityLog().Error("Texture not set!");
            return;
        }

        var mesh = new Mesh("randombezierplane", GenerateBezierPlane(), GenerateGridIndices(), texture: texture);

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

    private Vector3[] GenerateInitialControlPoints()
    {
        var points = new List<Vector3>();

        const float sizeX = 100f;
        const float sizeZ = 100f;
        const int gridCount = 4; // 4x4 control points for a single Bézier patch

        for (var row = 0; row < gridCount; row++)
        {
            var z = (row / 3f) * sizeZ;

            for (var col = 0; col < gridCount; col++)
            {
                points.Add(new Vector3((col / 3f) * sizeX, 0, z));
            }
        }

        return points.ToArray();
    }
    public List<Vertex> GenerateBezierPlane()
    {
        var controlPoints = GetPropertyValue<Vector3[]>("ControlPoints");
        if (controlPoints == null || controlPoints.Length == 0)
            return [];

        var quadSize = GetPropertyValue<int>("QuadSize");
        var textureScale = GetPropertyValue<Vector2>("TextureScale");

        List<Vertex> vertices = new();

        for (var i = 0; i <= quadSize; i++)
        {
            var u = i / (float)quadSize;

            for (var j = 0; j <= quadSize; j++)
            {
                var v = j / (float)quadSize;

                var position = EvaluateBezierSurface(controlPoints, u, v);
                var normal = EstimateNormal(controlPoints, u, v);
                var uv = new Vector2(u, v) * textureScale;

                vertices.Add(new Vertex
                {
                    Coordinates = position,
                    UV = uv,
                    Normal = normal
                });
            }
        }

        return vertices;
    }

    private static Vector3 EvaluateBezierSurface(Vector3[] cp, float u, float v)
    {
        var uCurve = new Vector3[4];

        for (var i = 0; i < 4; i++)
            uCurve[i] = Bezier1D(cp[i * 4], cp[i * 4 + 1], cp[i * 4 + 2], cp[i * 4 + 3], u);

        return Bezier1D(uCurve[0], uCurve[1], uCurve[2], uCurve[3], v);
    }

    private static Vector3 Bezier1D(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        var it = 1f - t;
        return it * it * it * p0 +
               3f * it * it * t * p1 +
               3f * it * t * t * p2 +
               t * t * t * p3;
    }

    private static Vector3 EstimateNormal(Vector3[] cp, float u, float v)
    {
        // Small delta for partial derivatives
        var delta = 0.001f;
        var p = EvaluateBezierSurface(cp, u, v);
        var pu = EvaluateBezierSurface(cp, u + delta, v) - p;
        var pv = EvaluateBezierSurface(cp, u, v + delta) - p;

        return Vector3.Normalize(Vector3.Cross(pv, pu));
    }
    public List<uint> GenerateGridIndices()
    {
        var quadSize = GetPropertyValue<int>("QuadSize");

        var indices = new List<uint>();

        for (var y = 0; y < quadSize; y++)
        {
            for (var x = 0; x < quadSize; x++)
            {
                var start = y * (quadSize + 1) + x;

                indices.Add((uint)start);
                indices.Add((uint)(start + 1));
                indices.Add((uint)(start + quadSize + 1));

                indices.Add((uint)(start + 1));
                indices.Add((uint)(start + quadSize + 2));
                indices.Add((uint)(start + quadSize + 1));
            }
        }

        return indices;
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
}