using System;
using Jellyfish.Render;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Jellyfish.Utils;

public readonly struct BoundingBox
{
    public Vector3 Center { get; }
    public Vector3 Size { get; }
    public Vector3 Max { get; }
    public Vector3 Min { get; }
    public float DiagonalLength => (Max - Min).Length;
    public float Radius => DiagonalLength * 0.5f;

    public BoundingBox(Vertex[] vertices)
    {
        if (vertices.Length == 0)
        {
            this = new BoundingBox(Vector3.Zero, Vector3.Zero);
            return;
        }

        var maxY = float.MinValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var minX = float.MaxValue;
        var maxZ = float.MinValue;
        var minZ = float.MaxValue;

        foreach (var vertex in vertices)
        {
            var coords = vertex.Coordinates;
            if (coords.X < minX)
                minX = coords.X;

            if (coords.X > maxX)
                maxX = coords.X;

            if (coords.Z < minZ)
                minZ = coords.Z;

            if (coords.Z > maxZ)
                maxZ = coords.Z;

            if (coords.Y < minY)
                minY = coords.Y;

            if (coords.Y > maxY)
                maxY = coords.Y;
        }

        var midX = (maxX + minX) / 2f;
        var midY = (maxY + minY) / 2f;
        var midZ = (maxZ + minZ) / 2f;

        Center = new Vector3(midX, midY, midZ);
        Size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
        Max = new Vector3(maxX, maxY, maxZ);
        Min = new Vector3(minX, minY, minZ);
    }

    public BoundingBox(List<Bone> bones, Matrix4[] boneTransforms)
    {
        if (bones.Count == 0)
        {
            this = new BoundingBox(new Vector3(5), new Vector3(-5));
            return;
        }

        var maxY = float.MinValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var minX = float.MaxValue;
        var maxZ = float.MinValue;
        var minZ = float.MaxValue;

        for (var i = 0; i < bones.Count; i++)
        {
            var coords = boneTransforms[i].ExtractTranslation();

            if (coords.X < minX)
                minX = coords.X;

            if (coords.X > maxX)
                maxX = coords.X;

            if (coords.Z < minZ)
                minZ = coords.Z;

            if (coords.Z > maxZ)
                maxZ = coords.Z;

            if (coords.Y < minY)
                minY = coords.Y;

            if (coords.Y > maxY)
                maxY = coords.Y;
        }

        // small offset since bones are too small
        var offset = 5;
        maxX += offset;
        maxY += offset;
        maxZ += offset;

        minX -= offset;
        minY -= offset;
        minZ -= offset;


        var midX = (maxX + minX) / 2f;
        var midY = (maxY + minY) / 2f;
        var midZ = (maxZ + minZ) / 2f;

        Center = new Vector3(midX, midY, midZ);
        Size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
        Max = new Vector3(maxX, maxY, maxZ);
        Min = new Vector3(minX, minY, minZ);
    }

    public BoundingBox(BoundingBox[] boxes)
    {
        if (boxes.Length == 0)
        {
            this = new BoundingBox(Vector3.Zero, Vector3.Zero);
            return;
        }

        var maxY = float.MinValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var minX = float.MaxValue;
        var maxZ = float.MinValue;
        var minZ = float.MaxValue;

        foreach (var box in boxes)
        {
            var min = box.Min;
            if (min.X < minX)
                minX = min.X;

            if (min.Z < minZ)
                minZ = min.Z;

            if (min.Y < minY)
                minY = min.Y;

            var max = box.Max;
            if (max.X > maxX)
                maxX = max.X;

            if (max.Z > maxZ)
                maxZ = max.Z;

            if (max.Y > maxY)
                maxY = max.Y;
        }

        var midX = (maxX + minX) / 2f;
        var midY = (maxY + minY) / 2f;
        var midZ = (maxZ + minZ) / 2f;

        Center = new Vector3(midX, midY, midZ);
        Size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
        Max = new Vector3(maxX, maxY, maxZ);
        Min = new Vector3(minX, minY, minZ);
    }

    public BoundingBox(Vector3 max, Vector3 min)
    {
        Center = (max + min) * 0.5f;
        Size = max - min;
        Min = min;
        Max = max;
    }

    public BoundingBox Translate(Matrix4 transform)
    {
        // transform the center, then project the half extents through the absolute rotation/scale part
        var center = Vector3.TransformPosition(Center, transform);
        var extents = Size * 0.5f;

        var newExtents = new Vector3(
            MathF.Abs(transform.M11) * extents.X + MathF.Abs(transform.M21) * extents.Y + MathF.Abs(transform.M31) * extents.Z,
            MathF.Abs(transform.M12) * extents.X + MathF.Abs(transform.M22) * extents.Y + MathF.Abs(transform.M32) * extents.Z,
            MathF.Abs(transform.M13) * extents.X + MathF.Abs(transform.M23) * extents.Y + MathF.Abs(transform.M33) * extents.Z);

        return new BoundingBox(center + newExtents, center - newExtents);
    }

    public bool IsPointInside(Vector3 point)
    {
        return point.X <= Max.X && point.Y <= Max.Y && point.Z <= Max.Z &&
               point.X >= Min.X && point.Y >= Min.Y && point.Z >= Min.Z;
    }
}