
using OpenTK.Mathematics;

namespace Jellyfish;

public static class MathExtensions
{
    public static Vector3 ToOpentkVector(this System.Numerics.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    public static Vector3 ToOpentkVector(this SteamAudio.IPL.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    public static Vector3 ToOpentkVector(this JoltPhysicsSharp.Double3 v)
    {
        return new Vector3((float)v.X, (float)v.Y, (float)v.Z);
    }

    public static System.Numerics.Vector3 ToNumericsVector(this Vector3 v)
    {
        return new System.Numerics.Vector3(v.X, v.Y, v.Z);
    }

    public static SteamAudio.IPL.Vector3 ToIplVector(this Vector3 v)
    {
        return new SteamAudio.IPL.Vector3(v.X, v.Y, v.Z);
    }

    public static Quaternion ToOpentkQuaternion(this System.Numerics.Quaternion v)
    {
        return new Quaternion(v.X, v.Y, v.Z, v.W);
    }

    public static System.Numerics.Quaternion ToNumericsQuaternion(this Quaternion v)
    {
        return new System.Numerics.Quaternion(v.X, v.Y, v.Z, v.W);
    }

    public static Matrix4 ToOpentkMatrix(this Assimp.Matrix4x4 mat)
    {
        return new Matrix4(mat.A1, mat.A2, mat.A3, mat.A4, mat.B1, mat.B2, mat.B3, mat.B4, mat.C1, mat.C2, mat.C3, mat.C4, mat.D1, mat.D2, mat.D3, mat.D4);
    }

    public static float[] ToFloatArray(this Matrix4 mat)
    {
        return new[]
        {
            mat.M11, mat.M12, mat.M13, mat.M14, 
            mat.M21, mat.M22, mat.M23, mat.M24, 
            mat.M31, mat.M32, mat.M33, mat.M34, 
            mat.M41, mat.M42, mat.M43, mat.M44
        };
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
}