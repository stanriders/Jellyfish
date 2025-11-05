
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

    public static VertexArray? QuadVertexArray { get; private set; }
    public static VertexArray? CubeVertexArray { get; private set; }

    public static void Initialize()
    {
        CubeVertexArray = new VertexArray(new VertexBuffer("Cube", CubeFloat), null, 3 * sizeof(float));

        GL.EnableVertexArrayAttrib(CubeVertexArray.Handle, 0);
        GL.VertexArrayAttribFormat(CubeVertexArray.Handle, 0, 3, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(CubeVertexArray.Handle, 0, 0);

        QuadVertexArray = new VertexArray(new VertexBuffer("Quad", Quad), null, 4 * sizeof(float));

        GL.EnableVertexArrayAttrib(QuadVertexArray.Handle, 0);
        GL.VertexArrayAttribFormat(QuadVertexArray.Handle, 0, 2, VertexAttribType.Float, false, 0);
        GL.VertexArrayAttribBinding(QuadVertexArray.Handle, 0, 0);
        
        GL.EnableVertexArrayAttrib(QuadVertexArray.Handle, 1);
        GL.VertexArrayAttribFormat(QuadVertexArray.Handle, 1, 2, VertexAttribType.Float, false, 2 * sizeof(float));
        GL.VertexArrayAttribBinding(QuadVertexArray.Handle, 1, 0);
    }

    public static void DrawQuad()
    {
        QuadVertexArray?.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        PerformanceMeasurment.Increment("DrawCalls");

        QuadVertexArray?.Unbind();
    }
}