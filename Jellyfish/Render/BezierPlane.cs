using System.Collections.Generic;
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public class BezierPlane : Mesh
{
    private const int size_x = 6;
    private const int size_y = 3;
    private readonly List<BezierCurve> curves = new(4);

    public BezierPlane()
    {
        Position = new Vector3(10, 10, 0);

        GeneratePlane();
        CreateBuffers();
        AddShader(new Main("test.png"));
    }

    public void GenerateBezierPlane()
    {
        const int step = 1;

        curves.Add(new BezierCurve(new Vector2(0, 0), new Vector2(size_x, 0)));
        curves.Add(new BezierCurve(new Vector2(0, 0), new Vector2(size_y, 0)));
    }

    public void GeneratePlane()
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();

        // First, build the data for the vertex buffer
        for (var y = 0; y < size_x; y++)
        for (var x = 0; x < size_y; x++)
        {
            // Position
            vertices.Add(new Vector3(x, y, 0));

            // Cheap normal using a derivative of the function.
            // The slope for X will be 2X, for Y will be 2Y.
            float xSlope = 2 * x;
            float ySlope = 2 * y;

            // Calculate the normal using the cross product of the slopes.
            float[] planeVectorX = { 1f, 0f, xSlope };
            float[] planeVectorY = { 0f, 1f, ySlope };
            float[] normalVector =
            {
                planeVectorX[1] * planeVectorY[2] - planeVectorX[2] * planeVectorY[1],
                planeVectorX[2] * planeVectorY[0] - planeVectorX[0] * planeVectorY[2],
                planeVectorX[0] * planeVectorY[1] - planeVectorX[1] * planeVectorY[0]
            };

            // Normalize the normal
            var length = new Vector3(normalVector[0], normalVector[1], normalVector[2]).Length;

            normals.Add(new Vector3(normalVector[0] / length, normalVector[1] / length,
                normalVector[2] / length));
        }

        // Now build the index data
        var indices = new List<uint>();
        for (var y = 0; y < size_y - 1; y++)
        {
            if (y > 0)
                // Degenerate begin: repeat first vertex
                indices.Add((uint)(y * size_y));

            for (var x = 0; x < size_x; x++)
            {
                // One part of the strip
                indices.Add((uint)(y * size_y + x));
                indices.Add((uint)((y + 1) * size_y + x));
            }

            if (y < size_y - 2)
                // Degenerate end: repeat last vertex
                indices.Add((uint)((y + 1) * size_y + (size_x - 1)));
        }

        MeshInfo.Vertices = vertices;
        MeshInfo.Indices = indices;
        MeshInfo.Normals = normals;
    }
}