using OpenTK.Mathematics;

namespace Jellyfish.Render.Lighting;

public class PointLight : ILightSource
{
    public float Quadratic { get; set; }

    public float Linear { get; set; }

    public float Constant { get; set; }
    public Vector3 Position { get; set; }

    public Color4 Color { get; set; }

    public bool Enabled { get; set; }
}