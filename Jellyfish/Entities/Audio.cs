using System.IO;
using Jellyfish.Audio;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("audio")]
public class Audio : BaseEntity
{
    public override bool DrawDevCone { get; set; } = true;
    private Sound? _handle;

    public Audio()
    {
        AddProperty<string>("Path", editable: false);
        AddProperty("Autoplay", false, false);
        AddProperty("UseAirAbsorption", true);
        AddProperty("Volume", 1.0f);

        AddAction("Play", Play);
    }

    public override void Load()
    {
        var path = GetPropertyValue<string>("Path");
        if (path == null)
        {
            EntityLog().Error("Null path!");
            return;
        }

        if (!File.Exists(path))
        {
            EntityLog().Error("Path {Path} doesn't exist!", path);
            return;
        }

        _handle = AudioManager.AddSound(path);

        if (_handle != null)
        {
            _handle.Position = GetPropertyValue<Vector3>("Position");
            _handle.UseAirAbsorption = GetPropertyValue<bool>("UseAirAbsorption");
            _handle.Volume = GetPropertyValue<float>("Volume");

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
            _handle.Volume = GetPropertyValue<float>("Volume");
        }

        base.Think();
    }

    public override void Unload()
    {
        _handle?.Dispose();

        base.Unload();
    }

    public void Play()
    {
        _handle?.Play();
    }
}