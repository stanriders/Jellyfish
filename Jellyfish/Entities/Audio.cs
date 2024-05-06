using Jellyfish.Audio;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("audio")]
public class Audio : BaseEntity
{
    private int? _handle;

    public Audio()
    {
        AddProperty<string>("Path");
        AddProperty("Autoplay", false);
    }

    public override void Load()
    {
        var position = GetPropertyValue<Vector3>("Position");

        var autoplay = GetPropertyValue<bool>("Autoplay");
        var path = GetPropertyValue<string>("Path");
        if (autoplay && path != null)
        {
            _handle = AudioManager.Play(path, position);
        }

        base.Load();
    }

    public override void Think()
    {
        if (_handle != null)
        {
            var position = GetPropertyValue<Vector3>("Position");
            AudioManager.Update(_handle.Value, position);
        }

        base.Think();
    }
}