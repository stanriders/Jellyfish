using OpenTK.Mathematics;

namespace Jellyfish.Utils;

public readonly struct Ray
{
    public Ray(Vector3 origin, Vector3 direction)
    {
        Origin = origin;
        Direction = direction.Normalized();
    }

    public Vector3 Origin { get; }
    public Vector3 Direction { get; }
}