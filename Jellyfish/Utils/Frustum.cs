using OpenTK.Mathematics;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Jellyfish.Utils;

public readonly struct Frustum
{
    public Vector4[] Planes { get; }
    public Vector3[] Corners { get; }
    public ReadOnlySpan<Vector3> NearCorners => new(Corners, 0, 4);
    public ReadOnlySpan<Vector3> FarCorners => new(Corners, 4, 4);
    public Vector3 Center { get; }
    public Vector3 NearPlaneCenter { get; }
    public Vector3 FarPlaneCenter { get; }

    private static readonly Vector3[] ClipCorners =
    [
        new(-1, -1, -1),
        new(+1, -1, -1),
        new(-1, +1, -1),
        new(+1, +1, -1),
        new(-1, -1, +1),
        new(+1, -1, +1),
        new(-1, +1, +1),
        new(+1, +1, +1),
    ];

    private static readonly int[,] EdgeIndices =
    {
        {0,1},{1,3},{3,2},{2,0}, // near face
        {4,5},{5,7},{7,6},{6,4}, // far face
        {0,4},{1,5},{2,6},{3,7}  // connections near->far
    };

    // axis similarity threshold: if normalized axes have dot > this, they are considered same direction
    private const float axis_dot_thresh = 0.9995f;
    private const float eps = 1e-6f;
    
    public Frustum(Matrix4 viewProjectionMatrix)
    {
        Planes = new Vector4[6];
        Corners = new Vector3[8];

        var m = viewProjectionMatrix;
        
        // Gribb-Hartmann, row-vector convention (v * M), GL depth range -1..1
        Planes[0] = new Vector4(m.M14 + m.M11, m.M24 + m.M21, m.M34 + m.M31, m.M44 + m.M41); // left
        Planes[1] = new Vector4(m.M14 - m.M11, m.M24 - m.M21, m.M34 - m.M31, m.M44 - m.M41); // right
        Planes[2] = new Vector4(m.M14 + m.M12, m.M24 + m.M22, m.M34 + m.M32, m.M44 + m.M42); // bottom
        Planes[3] = new Vector4(m.M14 - m.M12, m.M24 - m.M22, m.M34 - m.M32, m.M44 - m.M42); // top
        Planes[4] = new Vector4(m.M14 + m.M13, m.M24 + m.M23, m.M34 + m.M33, m.M44 + m.M43); // near
        Planes[5] = new Vector4(m.M14 - m.M13, m.M24 - m.M23, m.M34 - m.M33, m.M44 - m.M43); // far

        for (var i = 0; i < Planes.Length; i++)
        {
            var length = Planes[i].Xyz.Length;
            if (length > eps)
                Planes[i] /= length;
        }

        Matrix4.Invert(viewProjectionMatrix, out var invViewProj);

        for (var i = 0; i < Corners.Length; i++)
        {
            var corner = new Vector4(ClipCorners[i], 1.0f) * invViewProj;
            Corners[i] = corner.Xyz / corner.W;
        }

        Center = SpanAverage(Corners);
        NearPlaneCenter = SpanAverage(NearCorners);
        FarPlaneCenter = SpanAverage(FarCorners);
    }

    public bool IsInside(Vector3 center, float radius)
    {
        for (var i = 0; i < Planes.Length; i++)
        {
            var plane = Planes[i];
            
            // Distance from plane to sphere center:
            var distance = plane.X * center.X + plane.Y * center.Y + plane.Z * center.Z + plane.W;

            // If the center is more negative than -radius => completely outside
            if (distance < -radius)
                return false;
        }

        return true;
    }

    public bool IsInside(BoundingBox box)
    {
        foreach (var (a, b, c, d) in Planes)
        {
            var px = (a >= 0) ? box.Max.X : box.Min.X;
            var py = (b >= 0) ? box.Max.Y : box.Min.Y;
            var pz = (c >= 0) ? box.Max.Z : box.Min.Z;

            // Distance of that corner to the plane
            var dist = (a * px) + (b * py) + (c * pz) + d;

            // If "most positive" corner is behind plane, entire box is behind plane
            if (dist < 0f)
                return false;
        }

        // If not behind any plane => it’s at least partially in frustum
        return true;
    }

    public bool IsInside(Frustum b)
    {
        // gather candidate axes: plane normals first
        var axes = new List<Vector3>(12);

        foreach (var p in Planes)
        {
            axes.Add(new Vector3(p.X, p.Y, p.Z));
        }

        foreach (var p in b.Planes)
        {
            axes.Add(new Vector3(p.X, p.Y, p.Z));
        }

        // add cross-products of edges
        var edgesA = GetEdgeDirections(Corners);
        var edgesB = GetEdgeDirections(b.Corners);

        foreach (var ea in edgesA)
        {
            foreach (var eb in edgesB)
            {
                var axis = Vector3.Cross(ea, eb);
                if (axis.LengthSquared > eps)
                {
                    axes.Add(axis);
                }
            }
        }

        // deduplicate and normalize axes (skip near-zero)
        var normAxes = new List<Vector3>(axes.Count);
        foreach (var ax in axes)
        {
            if (ax.LengthSquared <= eps) continue;

            var n = Vector3.Normalize(ax);

            // canonical sign: make first non-zero component positive to avoid duplicated opposite directions
            if (MathF.Abs(n.X) > MathF.Abs(n.Y) ? n.X < 0f : n.Y < 0f)
                n = -n;

            // skip if similar axis already present
            var similar = false;
            foreach (var existing in normAxes)
            {
                if (MathF.Abs(Vector3.Dot(existing, n)) > axis_dot_thresh)
                {
                    similar = true;
                    break;
                }
            }
            if (!similar) normAxes.Add(n);
        }

        // SAT test: project both frustums onto every axis and see if intervals separate
        foreach (var axis in normAxes)
        {
            var (minA, maxA) = ProjectOntoAxis(Corners, axis);
            var (minB, maxB) = ProjectOntoAxis(b.Corners, axis);

            // if projection intervals do not overlap -> separating axis found
            if (maxA < minB - eps || maxB < minA - eps)
                return false;
        }

        // no separating axis found -> overlap
        return true;
    }

    private static IEnumerable<Vector3> GetEdgeDirections(Vector3[] corners)
    {
        for (var i = 0; i < EdgeIndices.GetLength(0); i++)
        {
            var a = corners[EdgeIndices[i, 0]];
            var b = corners[EdgeIndices[i, 1]];
            yield return b - a;
        }
    }

    private static (float min, float max) ProjectOntoAxis(Vector3[] corners, Vector3 axis)
    {
        // assumes axis is normalized (or at least direction-only is fine)
        var min = Vector3.Dot(corners[0], axis);
        var max = min;

        for (var i = 1; i < corners.Length; i++)
        {
            var d = Vector3.Dot(corners[i], axis);
            if (d < min) min = d;
            else if (d > max) max = d;
        }

        return (min, max);
    }

    private static Vector3 SpanAverage(ReadOnlySpan<Vector3> v)
    {
        Vector3 sum = default; 
        for (int i = 0; i < v.Length; i++) 
            sum += v[i]; 

        return sum / v.Length;
    }
}