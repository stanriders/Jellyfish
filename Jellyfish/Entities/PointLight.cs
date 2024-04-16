using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_point")]
public class PointLight : BaseEntity
{
    private Render.Lighting.PointLight _light = null!;
    public Color4 Color { get; set; }
    public bool Enabled { get; set; }
    public float Quadratic { get; set; } = 0.8f;
    public float Linear { get; set; } = 0.19f;
    public float Constant { get; set; } = 0.01f;
    
    public override void Load()
    {
        _light = new Render.Lighting.PointLight
        {
            Color = Color,
            Enabled = Enabled,
            Position = Position,
            Quadratic = Quadratic,
            Linear = Linear,
            Constant = Constant
        };
        LightManager.AddLight(_light);
    }

    public override void Think()
    {
        _light.Position = Position;
    }
}