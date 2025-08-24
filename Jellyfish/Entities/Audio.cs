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

        AddProperty("UseAirAbsorption", true, changeCallback: useAirAbsorption =>
        {
            if (_handle != null)
                _handle.UseAirAbsorption = useAirAbsorption;
        });

        AddProperty("Volume", 1.0f, changeCallback: volume =>
        {
            if (_handle != null) 
                _handle.Volume = volume;
        });

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

        _handle = Engine.AudioManager.AddSound(path);

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

    protected override void OnPositionChanged(Vector3 position)
    {
        if (_handle != null)
        {
            _handle.Position = position;
        }

        base.OnPositionChanged(position);
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