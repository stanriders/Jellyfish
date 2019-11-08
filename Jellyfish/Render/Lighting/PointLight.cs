
using OpenTK;
using OpenTK.Graphics;

namespace Jellyfish.Render.Lighting
{
    class PointLight : ILightSource
    {
        public Vector3 Position { get; set; }
        
        public Color4 Color { get; set; }

        public bool Enabled { get; set; }

        public float Quadratic { get; set; }

        public float Linear { get; set; }

        public float Constant { get; set; }
    }
}
