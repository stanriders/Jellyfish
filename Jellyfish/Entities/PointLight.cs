using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_point")]
public class PointLight : BaseEntity, ILightSource
{
    public PointLight()
    {
        AddProperty("Color", new Color4(255, 255, 255, 255));
        AddProperty("Ambient", new Color4(0.1f, 0.1f, 0.1f, 0));
        AddProperty("Enabled", true);
        AddProperty("Quadratic", 0.8f);
        AddProperty("Linear", 0.15f);
        AddProperty("Constant", 0.05f);
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