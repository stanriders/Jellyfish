using System;
using System.Linq;
using Jellyfish.Entities;
using OpenTK.Mathematics;

namespace Jellyfish.Utils;

public static class Trace
{
    public static BaseEntity? IntersectsEntity(Ray ray)
    {
        // skip entities that we are inside of
        var eligibleEntities = Engine.EntityManager.Entities
            .Where(x => !x.IsPointWithinBoundingBox(ray.Origin) && x.BoundingBox != null).ToArray();

        var minDistance = float.MaxValue;
        BaseEntity? bestEntity = null;

        foreach (var entity in eligibleEntities)
        {
            if (RayIntersectsAABB(new Ray(ray.Origin - entity.GetPropertyValue<Vector3>("Position"), ray.Direction), entity.BoundingBox!.Value, out var tmin))
            {
                if (minDistance > tmin)
                {
                    minDistance = tmin;
                    bestEntity = entity;
                }
            }
        }

        return bestEntity;
    }

    public static WorldMesh? IntersectsWorldMesh(Ray ray, out int faceIndex, out Vector3 hitPosition, out Vector3 hitNormal)
    {
        faceIndex = -1;
        hitPosition = Vector3.Zero;
        hitNormal = Vector3.Zero;

        var minDistance = float.MaxValue;
        WorldMesh? bestEntity = null;

        foreach (var entity in Engine.EntityManager.Entities.OfType<WorldMesh>())
        {
            var mesh = entity.GetPropertyValue<EditableMesh>("Mesh");
            if (mesh == null)
                continue;

            var transform = entity.Transform;
            var vertices = mesh.Vertices.Select(x => Vector3.TransformPosition(x, transform)).ToArray();

            for (var i = 0; i < mesh.Faces.Length; i++)
            {
                var indices = mesh.Faces[i].Indices;
                for (var j = 1; j < indices.Length - 1; j++)
                {
                    var a = vertices[indices[0]];
                    var b = vertices[indices[j]];
                    var c = vertices[indices[j + 1]];

                    if (RayIntersectsTriangle(ray, a, b, c, out var distance) && distance < minDistance)
                    {
                        minDistance = distance;
                        bestEntity = entity;
                        faceIndex = i;
                        hitPosition = ray.Origin + ray.Direction * distance;
                        hitNormal = MathUtils.CalculateNormal(a, b, c);
                    }
                }
            }
        }

        return bestEntity;
    }

    // Möller–Trumbore, only hits front faces (counter-clockwise winding)
    public static bool RayIntersectsTriangle(Ray ray, Vector3 a, Vector3 b, Vector3 c, out float distance)
    {
        distance = 0.0f;

        var edge1 = b - a;
        var edge2 = c - a;
        var p = Vector3.Cross(ray.Direction, edge2);
        var determinant = Vector3.Dot(edge1, p);
        if (determinant < 1e-6f)
            return false;

        var inverseDeterminant = 1.0f / determinant;
        var s = ray.Origin - a;
        var u = Vector3.Dot(s, p) * inverseDeterminant;
        if (u < 0 || u > 1)
            return false;

        var q = Vector3.Cross(s, edge1);
        var v = Vector3.Dot(ray.Direction, q) * inverseDeterminant;
        if (v < 0 || u + v > 1)
            return false;

        distance = Vector3.Dot(edge2, q) * inverseDeterminant;
        return distance > 0;
    }

    public static bool RayIntersectsAABB(Ray ray, BoundingBox box, out float tmin)
    {
        tmin = 0.0f;
        var tmax = float.MaxValue;

        for (var i = 0; i < 3; i++)
        {
            if (Math.Abs(ray.Direction[i]) < 1e-6)
            {
                // Ray is parallel to slab. No hit if origin not within the slab
                if (ray.Origin[i] < box.Min[i] || ray.Origin[i] > box.Max[i])
                    return false;
            }
            else
            {
                var invD = 1.0f / ray.Direction[i];
                var t0 = (box.Min[i] - ray.Origin[i]) * invD;
                var t1 = (box.Max[i] - ray.Origin[i]) * invD;
                if (t0 > t1)
                {
                    (t0, t1) = (t1, t0);
                }

                tmin = Math.Max(tmin, t0);
                tmax = Math.Min(tmax, t1);
                if (tmax < tmin)
                    return false;
            }
        }

        return true;
    }
}

