
using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Utils;

public static class CommonShapes
{
    public static float[] Quad =
    [
        // positions   // texCoords
        -1.0f, 1.0f, 0.0f, 1.0f,
        -1.0f, -1.0f, 0.0f, 0.0f,
        1.0f, -1.0f, 1.0f, 0.0f,

        -1.0f, 1.0f, 0.0f, 1.0f,
        1.0f, -1.0f, 1.0f, 0.0f,
        1.0f, 1.0f, 1.0f, 1.0f
    ];

    public static float[] CubeFloat =
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

    public static Vector3[] Cube =
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

    public static readonly (Vector3 Dir, Vector3 Up)[] CubeFaces =
    [
        (new Vector3( 1,  0,  0), new Vector3(0, 0, 1)),
        (new Vector3(-1,  0,  0), new Vector3(0, 0, 1)),
        (new Vector3( 0,  1,  0), new Vector3(0, 0, 1)),
        (new Vector3( 0, -1,  0), new Vector3(0, 0, 1)),
        (new Vector3( 0,  0,  1), new Vector3(0, 1, 0)),
        (new Vector3( 0,  0, -1), new Vector3(0, 1, 0))
    ];

    public static VertexArray? QuadVertexArray { get; private set; }
    public static VertexArray? CubeVertexArray { get; private set; }

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

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        PerformanceMeasurement.Increment("DrawCalls");

        QuadVertexArray?.Unbind();
    }
}