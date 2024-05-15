using SteamAudio;

namespace Jellyfish.Audio;

public static class IPLExtensions
{
    public static bool Equals(this IPL.Vector3 v1, IPL.Vector3 v2)
    {
        return (v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z);
    }
}