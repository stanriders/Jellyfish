﻿using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.Utils;

public readonly struct BoundingBox
{
    public Vector3 Center { get; }
    public Vector3 Size { get; }
    public Vector3 Max { get; }
    public Vector3 Min { get; }
    public float Length { get; }

    public BoundingBox(Vertex[] vertices)
    {
        var maxY = 0f;
        var minY = 0f;
        var maxX = 0f;
        var minX = 0f;
        var maxZ = 0f;
        var minZ = 0f;

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
        Length = (Max - Min).Length;
    }

    public BoundingBox(BoundingBox[] boxes)
    {
        var maxY = 0f;
        var minY = 0f;
        var maxX = 0f;
        var minX = 0f;
        var maxZ = 0f;
        var minZ = 0f;

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
        Length = (Max - Min).Length;
    }

    public BoundingBox(Vector3 max, Vector3 min)
    {
        var midX = (max.X + min.X) / 2f;
        var midY = (max.Y + min.Y) / 2f;
        var midZ = (max.Z + min.Z) / 2f;

        Center = new Vector3(midX, midY, midZ);
        Size = new Vector3(max.X - min.X, max.Y - min.Y, max.Z - min.Z);
        Min = min;
        Max = max;
        Length = (Max - Min).Length;
    }

    public BoundingBox Translate(Matrix4 transform)
    {
        var transformedMax = Vector3.TransformVector(Max, transform);
        var transformedMin = Vector3.TransformVector(Min, transform);

        var newMax = new Vector3(transformedMin.X > transformedMax.X ? transformedMin.X : transformedMax.X, 
            transformedMin.Y > transformedMax.Y ? transformedMin.Y : transformedMax.Y, 
            transformedMin.Z > transformedMax.Z ? transformedMin.Z : transformedMax.Z);

        var newMin = new Vector3(transformedMin.X < transformedMax.X ? transformedMin.X : transformedMax.X,
            transformedMin.Y < transformedMax.Y ? transformedMin.Y : transformedMax.Y,
            transformedMin.Z < transformedMax.Z ? transformedMin.Z : transformedMax.Z);

        return new BoundingBox(newMax, newMin);
    }

    public bool IsPointInside(Vector3 point)
    {
        return point.X < Max.X && point.Y < Max.Y && point.Z < Max.Z &&
               point.X > Min.X && point.Y > Min.Y && point.Z > Min.Z;
    }
}