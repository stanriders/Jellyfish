using System;
using System.Linq;
using Jellyfish.Entities;
using OpenTK.Mathematics;
using Jellyfish.Utils;

namespace Jellyfish;

public static class Trace
{
    public static BaseEntity? IntersectsEntity(Ray ray)
    {
        if (EntityManager.Entities == null)
            return null;

        // skip entities that we are inside of
        var eligibleEntities = EntityManager.Entities
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

