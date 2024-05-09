using System.IO;
using Jellyfish.Audio;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Entities;

[Entity("audio")]
public class Audio : BaseEntity
{
    private Sound? _handle;

    public Audio()
    {
        AddProperty<string>("Path");
        AddProperty("Autoplay", false);
        AddProperty("UseAirAbsorption", true);
    }

    public override void Load()
    {
        var path = GetPropertyValue<string>("Path");
        if (path == null)
        {
            Log.Error("[Sound] Null path!");
            return;
        }

        if (!File.Exists(path))
        {
            Log.Error("[Sound] Path {Path} doesn't exist!", path);
            return;
        }

        _handle = AudioManager.AddSound(path);

        if (_handle != null)
        {
            _handle.Position = GetPropertyValue<Vector3>("Position");
            _handle.UseAirAbsorption = GetPropertyValue<bool>("UseAirAbsorption");

            var autoplay = GetPropertyValue<bool>("Autoplay");
            if (autoplay)
            {
                _handle.Play();
            }
        }

        base.Load();
    }

    public override void Think()
    {
        if (_handle != null)
        {
            _handle.Position = GetPropertyValue<Vector3>("Position");
            _handle.UseAirAbsorption = GetPropertyValue<bool>("UseAirAbsorption");
        }

        base.Think();
    }
}