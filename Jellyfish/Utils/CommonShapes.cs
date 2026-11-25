
using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Utils;

public static class CommonShapes
{
    private static readonly float[] Quad =
    [
        // positions   // texCoords
        -1.0f, 1.0f, 0.0f, 1.0f,
        -1.0f, -1.0f, 0.0f, 0.0f,
        1.0f, -1.0f, 1.0f, 0.0f,

        -1.0f, 1.0f, 0.0f, 1.0f,
        1.0f, -1.0f, 1.0f, 0.0f,
        1.0f, 1.0f, 1.0f, 1.0f
    ];

    private static readonly float[] CubeFloat =
    [
        // positions          
        -1.0f,  1.0f, -1.0f,
        -1.0f, -1.0f, -1.0f,
        1.0f, -1.0f, -1.0f,
        1.0f, -1.0f, -1.0f,
        1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f, -1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

        1.0f, -1.0f, -1.0f,
        1.0f, -1.0f,  1.0f,
        1.0f,  1.0f,  1.0f,
        1.0f,  1.0f,  1.0f,
        1.0f,  1.0f, -1.0f,
        1.0f, -1.0f, -1.0f,

        -1.0f, -1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
        1.0f,  1.0f,  1.0f,
        1.0f,  1.0f,  1.0f,
        1.0f, -1.0f,  1.0f,
        -1.0f, -1.0f,  1.0f,

        -1.0f,  1.0f, -1.0f,
        1.0f,  1.0f, -1.0f,
        1.0f,  1.0f,  1.0f,
        1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f,  1.0f,
        -1.0f,  1.0f, -1.0f,

        -1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
        1.0f, -1.0f, -1.0f,
        1.0f, -1.0f, -1.0f,
        -1.0f, -1.0f,  1.0f,
        1.0f, -1.0f,  1.0f
    ];

    private static readonly Vector3[] Cube =
    [
        // positions
        new(-1.0f, 1.0f, -1.0f),
        new(-1.0f, -1.0f, -1.0f),
        new(1.0f, -1.0f, -1.0f),
        new(1.0f, -1.0f, -1.0f),
        new(1.0f, 1.0f, -1.0f),
        new(-1.0f, 1.0f, -1.0f),

        new(-1.0f, -1.0f, 1.0f),
        new(-1.0f, -1.0f, -1.0f),
        new(-1.0f, 1.0f, -1.0f),
        new(-1.0f, 1.0f, -1.0f),
        new(-1.0f, 1.0f, 1.0f),
        new(-1.0f, -1.0f, 1.0f),

        new(1.0f, -1.0f, -1.0f),
        new(1.0f, -1.0f, 1.0f),
        new(1.0f, 1.0f, 1.0f),
        new(1.0f, 1.0f, 1.0f),
        new(1.0f, 1.0f, -1.0f),
        new(1.0f, -1.0f, -1.0f),

        new(-1.0f, -1.0f, 1.0f),
        new(-1.0f, 1.0f, 1.0f),
        new(1.0f, 1.0f, 1.0f),
        new(1.0f, 1.0f, 1.0f),
        new(1.0f, -1.0f, 1.0f),
        new(-1.0f, -1.0f, 1.0f),

        new(-1.0f, 1.0f, -1.0f),
        new(1.0f, 1.0f, -1.0f),
        new(1.0f, 1.0f, 1.0f),
        new(1.0f, 1.0f, 1.0f),
        new(-1.0f, 1.0f, 1.0f),
        new(-1.0f, 1.0f, -1.0f),

        new(-1.0f, -1.0f, -1.0f),
        new(-1.0f, -1.0f, 1.0f),
        new(1.0f, -1.0f, 1.0f),
        new(1.0f, -1.0f, 1.0f),
        new(1.0f, -1.0f, -1.0f),
        new(-1.0f, -1.0f, -1.0f)
    ];

    private static readonly (Vector3 Dir, Vector3 Up)[] CubemapViews =
    [
        (new Vector3( 1,  0,  0), new Vector3(0, -1,  0)), // +X
        (new Vector3(-1,  0,  0), new Vector3(0, -1,  0)), // -X
        (new Vector3( 0,  1,  0), new Vector3(0,  0,  1)), // +Y
        (new Vector3( 0, -1,  0), new Vector3(0,  0, -1)), // -Y
        (new Vector3( 0,  0,  1), new Vector3(0, -1,  0)), // +Z
        (new Vector3( 0,  0, -1), new Vector3(0, -1,  0)) // -Z
    ];

    private static VertexArray? QuadVertexArray { get; set; }
    private static VertexArray? CubeVertexArray { get; set; }

    public static void Initialize()
    {
        CubeVertexArray = new VertexArray(new VertexBuffer("Cube", CubeFloat), null, 3 * sizeof(float));
        CubeVertexArray.AddAttribute(0, 3, 0);

        QuadVertexArray = new VertexArray(new VertexBuffer("Quad", Quad), null, 4 * sizeof(float));
        QuadVertexArray.AddAttribute(0, 2, 0);
        QuadVertexArray.AddAttribute(1, 2, 2 * sizeof(float));
    }

    public static void DrawQuad()
    {
        QuadVertexArray?.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, Quad.Length);
        PerformanceMeasurement.Increment("DrawCalls");

        QuadVertexArray?.Unbind();
    }

    public static void DrawCube()
    {
        CubeVertexArray?.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, Cube.Length);
        PerformanceMeasurement.Increment("DrawCalls");

        CubeVertexArray?.Unbind();
    }

    public static Matrix4 GetCubemapViewMatrix(Vector3 position, int face)
    {
        return Matrix4.LookAt(position, position + CubemapViews[face].Dir, CubemapViews[face].Up);
    }
}