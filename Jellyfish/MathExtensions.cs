
namespace Jellyfish
{
    public static class MathExtensions
    {
        public static OpenTK.Mathematics.Vector3 ToOpentkVector(this System.Numerics.Vector3 v)
        {
            return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
        }

        public static OpenTK.Mathematics.Vector3 ToOpentkVector(this SteamAudio.IPL.Vector3 v)
        {
            return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
        }

        public static System.Numerics.Vector3 ToNumericsVector(this OpenTK.Mathematics.Vector3 v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static SteamAudio.IPL.Vector3 ToIplVector(this OpenTK.Mathematics.Vector3 v)
        {
            return new SteamAudio.IPL.Vector3(v.X, v.Y, v.Z);
        }

        public static OpenTK.Mathematics.Quaternion ToOpentkQuaternion(this System.Numerics.Quaternion v)
        {
            return new OpenTK.Mathematics.Quaternion(v.X, v.Y, v.Z, v.W);
        }

        public static System.Numerics.Quaternion ToNumericsQuaternion(this OpenTK.Mathematics.Quaternion v)
        {
            return new System.Numerics.Quaternion(v.X, v.Y, v.Z, v.W);
        }
    }
}
