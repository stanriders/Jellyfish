using System.Collections.Generic;
using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_sun")]
public class Sun : BaseEntity, ILightSource
{
    public override IReadOnlyList<EntityProperty> EntityProperties { get; } = new List<EntityProperty>
    {
        new EntityProperty<Color4>("Color", new Color4(255,255,255,255)),
        new EntityProperty<Color4>("Ambient", new Color4(0.1f,0.1f,0.1f,0)),
        new EntityProperty<Vector3>("Direction", new Vector3(0f,1f,0f)),
        new EntityProperty<bool>("Enabled", true)
    };

    public override void Load()
    {
        LightManager.AddLight(this);
    }

    public Color4 Color => GetPropertyValue<Color4>("Color");
    public Color4 Ambient => GetPropertyValue<Color4>("Ambient");
    public bool Enabled => GetPropertyValue<bool>("Enabled");
}