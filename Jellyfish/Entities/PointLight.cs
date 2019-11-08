
using Jellyfish.Render.Lighting;
using OpenTK.Graphics;

namespace Jellyfish.Entities
{
    public class PointLight : BaseEntity
    {
        public Color4 Color { get; set; }
        public bool Enabled { get; set; }
        public float Quadratic { get; set; } = 0.8f;
        public float Linear { get; set; } = 0.19f;
        public float Constant { get; set; } = 0.01f;

        private Render.Lighting.PointLight light;

        public override void Load()
        {
            light = new Render.Lighting.PointLight()
            {
                Color = Color,
                Enabled = Enabled,
                Position = Position,
                Quadratic = Quadratic,
                Linear = Linear,
                Constant = Constant
            };
            LightManager.AddLight(light);
        }
    }
}
