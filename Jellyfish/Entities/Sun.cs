using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_sun")]
public class Sun : BaseEntity, ILightSource
{
    public Sun()
    {
        AddProperty("Color", new Color4(255, 255, 255, 255));
        AddProperty("Ambient", new Color4(0.1f, 0.1f, 0.1f, 0));
        AddProperty("Enabled", true);
        AddProperty("Direction", new Vector3(0f, 1f, 0f));
    }

    public override void Load()
    {
        LightManager.AddLight(this);
    }

    public Vector3 Position => GetPropertyValue<Vector3>("Position");
    public Color4 Color => GetPropertyValue<Color4>("Color");
    public Color4 Ambient => GetPropertyValue<Color4>("Ambient");
    public bool Enabled => GetPropertyValue<bool>("Enabled");
}