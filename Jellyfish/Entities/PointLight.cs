using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Jellyfish.Entities;

[Entity("light_point")]
public class PointLight : BaseEntity
{
    private Render.Lighting.PointLight _light = null!;

    public override IReadOnlyList<EntityProperty> EntityProperties { get; } = new List<EntityProperty>
    {
        new EntityProperty<Color4>("Color", new Color4(255,255,255,255)),
        new EntityProperty<bool>("Enabled", true),
        new EntityProperty<float>("Quadratic", 0.8f),
        new EntityProperty<float>("Linear", 0.15f),
        new EntityProperty<float>("Constant", 0.05f)
    };

    public override void Load()
    {
        _light = new Render.Lighting.PointLight
        {
            Position = Position,
            Color = GetPropertyValue<Color4>("Color"),
            Enabled = GetPropertyValue<bool>("Enabled"),
            Quadratic = GetPropertyValue<float>("Quadratic"),
            Linear = GetPropertyValue<float>("Linear"),
            Constant = GetPropertyValue<float>("Constant")
        };
        LightManager.AddLight(_light);
    }

    public override void Think()
    {
        _light.Position = Position;
    }
}