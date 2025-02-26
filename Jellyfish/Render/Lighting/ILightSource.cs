using OpenTK.Mathematics;

namespace Jellyfish.Render.Lighting;

public interface ILightSource
{
    Vector3 Position { get; }
    Quaternion Rotation { get; }

    Color4<Rgba> Color { get; }
    Color4<Rgba> Ambient { get; }

    bool Enabled { get; }
    bool UseShadows { get; }

    float NearPlane { get; }
    float FarPlane { get; }

    Matrix4 Projection { get; }
}