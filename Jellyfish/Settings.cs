using System.IO;
using Jellyfish.Audio;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;

namespace Jellyfish;

public class Settings
{
    // todo: write a converter that will exclude computed properties instead??
    public record struct Integer2Serializable
    {
        /// <summary>The X component of the Vector2i.</summary>
        public int X;
        /// <summary>The Y component of the Vector2i.</summary>
        public int Y;

        public Integer2Serializable(int x, int y)
        {
            X = x; 
            Y = y;
        }

        public static implicit operator Vector2i(Integer2Serializable vec) => new(vec.X, vec.Y);

        public override string ToString()
        {
            return $"{X}x{Y}";
        }
    }

    public class AudioConfig
    {
        public float Volume { get; set; } = 1.0f;
    }

    public class VideoConfig
    {
        public Integer2Serializable WindowSize { get; set; } = new(1920, 1080);
        public bool Fullscreen { get; set; } = false;
    }

    public AudioConfig Audio { get; set; } = new();
    public VideoConfig Video { get; set; } = new();

    private const string path = "settings.json";

    private static Settings? instance;

    public static Settings Instance
    {
        get
        {
            if (instance == null)
            {
                if (!File.Exists(path))
                {
                    instance = new Settings();
                    File.WriteAllText(path, JsonConvert.SerializeObject(instance, settings: new JsonSerializerSettings {ReferenceLoopHandling = ReferenceLoopHandling.Ignore}));
                    return instance;
                }

                instance = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path)) ?? new Settings();
            }

            return instance;
        }
        set { instance = value; Save(); }
    }

    public static void Save()
    {
        File.WriteAllText(path, JsonConvert.SerializeObject(instance));
    }
}