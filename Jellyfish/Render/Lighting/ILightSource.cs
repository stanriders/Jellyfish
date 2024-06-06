using OpenTK.Mathematics;

namespace Jellyfish.Render.Lighting;

public interface ILightSource
{
    Vector3 Position { get; }
    Quaternion Rotation { get; }

    Color4 Color { get; }
    Color4 Ambient { get; }

    bool Enabled { get; }
    bool UseShadows { get; }

    float NearPlane { get; }
    float FarPlane { get; }

    Matrix4 Projection { get; }
}