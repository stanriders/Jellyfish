using OpenTK.Mathematics;

namespace Jellyfish.Utils;

public static class MathUtils
{
    public static Vector3 CalculateNormal(Vector3 v1, Vector3 v2, Vector3 v3, bool inverted = false)
    {
        var edge1 = v2 - v1;
        var edge2 = v3 - v1;

        if (inverted)
            return Vector3.Cross(edge2, edge1).Normalized();

        return Vector3.Cross(edge1, edge2).Normalized();
    }
}