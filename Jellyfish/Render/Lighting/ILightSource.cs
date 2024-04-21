using OpenTK.Mathematics;

namespace Jellyfish.Render.Lighting;

public interface ILightSource
{
    Vector3 Position { get; }

    Color4 Color { get; }
    Color4 Ambient { get; }

    bool Enabled { get; }
}