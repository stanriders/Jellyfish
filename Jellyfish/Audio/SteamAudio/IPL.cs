namespace Jellyfish.Audio.SteamAudio
{
	/// <summary>
	/// Contains all of the Phonon Library's functions.
	/// </summary>
	public static partial class IPL
	{
		public const string Library = "phonon.dll";

		public partial struct Vector3
		{
			public Vector3(float x, float y, float z)
			{
				X = x;
				Y = y;
				Z = z;
            }

            public static bool Equals(Vector3 v1, Vector3 v2)
            {
                return (v1.X == v2.X && v1.Y == v2.Y && v1.Z == v2.Z);
            }
        }
    }
}
