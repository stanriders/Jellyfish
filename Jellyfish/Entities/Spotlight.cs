using Jellyfish.Utils;
using OpenTK.Mathematics;
using System;

namespace Jellyfish.Entities;

[Entity("light_spot")]
public class Spotlight : LightEntity, IHaveFrustum
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
    public override float NearPlane => 1f;
    public override float FarPlane => GetPropertyValue<float>("FarPlane");
    public override int ProjectionCount => 1;

    private Matrix4? _projection;
    public override Matrix4 Projection(int index)
    {
        if (_projection != null)
        {
            return _projection.Value;
        }

        var lightProjection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Math.Min(89.9f, GetPropertyValue<float>("OuterCone"))) * 2.0f, 1.0f, NearPlane, FarPlane);
        var lightView = Matrix4.LookAt(Position, Position + Vector3.Transform(-Vector3.UnitY, Rotation), Vector3.UnitZ);

        var finalProjection = lightView * lightProjection;
        _projection = finalProjection;

        return finalProjection;
    }

    public override void ClearProjectionCache()
    {
        _projection = null;
    }

    public Frustum GetFrustum()
    {
        return new Frustum(Projection(0));
    }
}