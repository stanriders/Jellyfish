
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
        new(1.0f, -1.0f, -1.0f),
        new(1.0f, -1.0f, -1.0f),
        new(-1.0f, -1.0f, 1.0f),
        new(1.0f, -1.0f, 1.0f)
    ];
}