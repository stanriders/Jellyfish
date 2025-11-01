using Jellyfish.Render;
using OpenTK.Mathematics;
using System.Buffers;
using System.Collections.Generic;

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

    public BoundingBox(List<Bone> bones, Matrix4[] boneTransforms)
    {
        var maxY = 0f;
        var minY = 0f;
        var maxX = 0f;
        var minX = 0f;
        var maxZ = 0f;
        var minZ = 0f;

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
        var corners = ArrayPool<Vector3>.Shared.Rent(8);
        corners[0] = new Vector3(Min.X, Min.Y, Min.Z);
        corners[1] = new Vector3(Max.X, Min.Y, Min.Z);
        corners[2] = new Vector3(Min.X, Max.Y, Min.Z);
        corners[3] = new Vector3(Max.X, Max.Y, Min.Z);
        corners[4] = new Vector3(Min.X, Min.Y, Max.Z);
        corners[5] = new Vector3(Max.X, Min.Y, Max.Z);
        corners[6] = new Vector3(Min.X, Max.Y, Max.Z);
        corners[7] = new Vector3(Max.X, Max.Y, Max.Z);

        for (var i = 0; i < corners.Length; i++)
            corners[i] = Vector3.TransformPosition(corners[i], transform);

        var newMin = new Vector3(float.MaxValue);
        var newMax = new Vector3(float.MinValue);

        foreach (var corner in corners)
        {
            newMin = Vector3.ComponentMin(newMin, corner);
            newMax = Vector3.ComponentMax(newMax, corner);
        }
        ArrayPool<Vector3>.Shared.Return(corners);

        return new BoundingBox(newMax, newMin);
    }

    public bool IsPointInside(Vector3 point)
    {
        return point.X < Max.X && point.Y < Max.Y && point.Z < Max.Z &&
               point.X > Min.X && point.Y > Min.Y && point.Z > Min.Z;
    }
}