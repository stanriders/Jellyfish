using OpenTK.Mathematics;
using System.Collections.Generic;

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

    int ProjectionCount { get; }
    Matrix4 Projection(int index);

    bool UsePcss { get; }

    int ShadowResolution => 2048;
}