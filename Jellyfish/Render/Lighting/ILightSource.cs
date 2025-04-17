using OpenTK.Mathematics;

namespace Jellyfish.Render.Lighting;

public interface ILightSource
{
    Vector3 Position { get; }
    Quaternion Rotation { get; }

    Color3<Rgb> Color { get; }
    Color3<Rgb> Ambient { get; }
    float Brightness { get; }

    bool Enabled { get; }
    bool UseShadows { get; }

    float NearPlane { get; }
    float FarPlane { get; }

    Matrix4[] Projections { get; }

    bool UsePcss { get; }

    int ShadowResolution => 2048;
}