using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_spot")]
public class Spotlight : BaseEntity, ILightSource
{
    public override bool DrawDevCone { get; set; } = true;

    public Spotlight()
    {
        AddProperty("Color", new Color4(255, 255, 255, 255));
        AddProperty("Ambient", new Color4(0.1f, 0.1f, 0.1f, 0));
        AddProperty("Enabled", true);
        AddProperty("Shadows", true);
        AddProperty("Quadratic", 0.8f);
        AddProperty("Linear", 0.15f);
        AddProperty("Constant", 0.05f);
        AddProperty("Cone", 12f);
        AddProperty("OuterCone", 25f);
    }

    public override void Load()
    {
        base.Load();
        LightManager.AddLight(this);
    }

    public Vector3 Position => GetPropertyValue<Vector3>("Position");
    public Quaternion Rotation => GetPropertyValue<Quaternion>("Rotation");
    public Color4 Color => GetPropertyValue<Color4>("Color");
    public Color4 Ambient => GetPropertyValue<Color4>("Ambient");
    public bool Enabled => GetPropertyValue<bool>("Enabled");
    public bool UseShadows => GetPropertyValue<bool>("Shadows");
    public float NearPlane => 0.1f;
    public float FarPlane => 500f;
    public Matrix4 Projection 
    {
        get
        {
            var lightProjection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(GetPropertyValue<float>("OuterCone")), 1.0f, NearPlane, FarPlane);
            var lightView = Matrix4.LookAt(Position, Position + Vector3.Transform(-Vector3.UnitY, Rotation), Vector3.UnitY);
            return lightView * lightProjection;
        }
    }
}