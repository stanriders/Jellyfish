using OpenTK.Mathematics;

namespace Jellyfish.Render.Lighting;

public interface ILightSource
{
    Vector3 Position { get; set; }

    Color4 Color { get; set; }

    bool Enabled { get; set; }
}