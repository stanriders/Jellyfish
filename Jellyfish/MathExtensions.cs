
using System.Linq;
using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish;

public static class MathExtensions
{
    public static System.Numerics.Vector3 ToNumericsVector(this Vector3 v)
    {
        return (System.Numerics.Vector3)v;
    }

    public static System.Numerics.Vector4 ToNumericsVector(this Color4<Rgba> v)
    {
        return new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);
    }

    public static SteamAudio.IPL.Vector3 ToIplVector(this Vector3 v)
    {
        return new SteamAudio.IPL.Vector3(v.X, v.Y, v.Z);
    }

    public static System.Numerics.Quaternion ToNumericsQuaternion(this Quaternion v)
    {
        return (System.Numerics.Quaternion)v;
    }

    public static float[] ToFloatArray(this Matrix4 mat)
    {
        return
        [
            mat.M11, mat.M12, mat.M13, mat.M14, 
            mat.M21, mat.M22, mat.M23, mat.M24, 
            mat.M31, mat.M32, mat.M33, mat.M34, 
            mat.M41, mat.M42, mat.M43, mat.M44
        ];
    }

    public static Matrix4 ToOpentkMatrix(this Assimp.Matrix4x4 m)
    {
        return new Matrix4(
            m.A1, m.B1, m.C1, m.D1,
            m.A2, m.B2, m.C2, m.D2,
            m.A3, m.B3, m.C3, m.D3,
            m.A4, m.B4, m.C4, m.D4
        );
    }

    public static Matrix4 ToMatrix(this float[] mat)
    {
        return new Matrix4(
            mat[0], mat[1], mat[2], mat[3], 
            mat[4], mat[5], mat[6], mat[7], 
            mat[8], mat[9], mat[10], mat[11], 
            mat[12], mat[13], mat[14], mat[15]);
    }

    public static Vector3 ToDegrees(this Vector3 vector)
    {
        return new Vector3(MathHelper.RadiansToDegrees(vector.X), 
            MathHelper.RadiansToDegrees(vector.Y),
            MathHelper.RadiansToDegrees(vector.Z));
    }

    public static Vector2 ToScreenspace(this Vector3 vector)
    {
        var clipSpacePos = new Vector4(vector, 1.0f) * Engine.MainViewport.GetViewMatrix() * Engine.MainViewport.GetProjectionMatrix();

        if (clipSpacePos.W > 0.0f)
        {
            clipSpacePos.X /= clipSpacePos.W;
            clipSpacePos.Y /= clipSpacePos.W;
            clipSpacePos.Z /= clipSpacePos.W;
        }

        float x = (clipSpacePos.X * 0.5f + 0.5f) * Engine.MainViewport.Size.X;
        float y = (1.0f - (clipSpacePos.Y * 0.5f + 0.5f)) * Engine.MainViewport.Size.Y;

        return new Vector2(x, y);
    }

    public static System.Numerics.Vector2 ToScreenspace(this System.Numerics.Vector3 vector)
    {
        var clipSpacePos = new Vector4((Vector3)vector, 1.0f) * Engine.MainViewport.GetViewMatrix() * Engine.MainViewport.GetProjectionMatrix();

        if (clipSpacePos.W > 0.0f)
        {
            clipSpacePos.X /= clipSpacePos.W;
            clipSpacePos.Y /= clipSpacePos.W;
            clipSpacePos.Z /= clipSpacePos.W;
        }

        float x = (clipSpacePos.X * 0.5f + 0.5f) * Engine.MainViewport.Size.X;
        float y = (1.0f - (clipSpacePos.Y * 0.5f + 0.5f)) * Engine.MainViewport.Size.Y;

        return new System.Numerics.Vector2(x, y);
    }

    public static Vector3 Average(this Vector3[] points)
    {
        var sum = points.Aggregate(Vector3.Zero, (current, p) => current + p);
        return sum / points.Length;
    }
}