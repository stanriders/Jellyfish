﻿using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_spot")]
public class Spotlight : LightEntity
{
    public override bool DrawDevCone { get; set; } = true;

    public Spotlight()
    {
        AddProperty("Quadratic", 0.8f);
        AddProperty("Linear", 0.15f);
        AddProperty("Constant", 0.05f);
        AddProperty("Cone", 12f);
        AddProperty("OuterCone", 25f);
        AddProperty("FarPlane", 300f);
    }

    public override int ShadowResolution => 1024;
    public override float NearPlane => 0.1f;
    public override float FarPlane => GetPropertyValue<float>("FarPlane");

    public override Matrix4[] Projections
    {
        get
        {
            var lightProjection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(GetPropertyValue<float>("OuterCone")) * 2.0f, 1.0f, NearPlane, FarPlane);
            var lightView = Matrix4.LookAt(Position, Position + Vector3.Transform(-Vector3.UnitY, Rotation), Vector3.UnitY);
            return [lightView * lightProjection];
        }
    }

}