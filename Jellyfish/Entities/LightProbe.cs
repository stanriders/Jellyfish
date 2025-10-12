using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_probe")]
public class LightProbe : BaseEntity
{
    private Render.Lighting.LightProbe? _probe;

    public override void Load()
    {
        base.Load();
        _probe = Engine.Renderer.ImageBasedLighting?.AddProbe();
        if (_probe != null)
        {
            _probe.Position = GetPropertyValue<Vector3>("Position");
        }
    }

    protected override void OnPositionChanged(Vector3 position)
    {
        if (_probe != null)
        {
            _probe.Position = position;
        }

        base.OnPositionChanged(position);
    }

    public override void Unload()
    {
        if (_probe != null)
        {
            Engine.Renderer.ImageBasedLighting?.RemoveProbe(_probe);
        }

        base.Unload();
    }
}