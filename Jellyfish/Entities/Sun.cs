using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;
namespace Jellyfish.Entities;

[Entity("light_sun")]
public class Sun : BaseEntity, ILightSource
{
    public Sun()
    {
        AddProperty("Color", new Color4<Rgba>(1, 1, 1, 1));
        AddProperty("Ambient", new Color4<Rgba>(0.1f, 0.1f, 0.1f, 0));
        AddProperty("Enabled", true);
        AddProperty("Shadows", true);
    }

    public override void Load()
    {
        base.Load();
        LightManager.AddLight(this);
    }

    public override void Unload()
    {
        LightManager.RemoveLight(this);
        base.Unload();
    }

    public Vector3 Position => GetPropertyValue<Vector3>("Position");
    public Quaternion Rotation => GetPropertyValue<Quaternion>("Rotation");
    public Color4<Rgba> Color => GetPropertyValue<Color4<Rgba>>("Color");
    public Color4<Rgba> Ambient => GetPropertyValue<Color4<Rgba>>("Ambient");
    public bool Enabled => GetPropertyValue<bool>("Enabled");
    public bool UseShadows => GetPropertyValue<bool>("Shadows");
    public float NearPlane => 1f;
    public float FarPlane => 4100f;

    public Matrix4 Projection
    {
        get
        {
            var position = new Vector3(0f, 4000f, 0f);

            if (Player.Instance != null)
            {
                position = Player.Instance.GetPropertyValue<Vector3>("Position") + Vector3.Transform(GetPropertyValue<Vector3>("Position"), Rotation);
            }

            var lightProjection = Matrix4.CreateOrthographic(position.Y, position.Y, NearPlane, position.Y * 1.5f);
            var lightView = Matrix4.LookAt(position, position + Vector3.Transform(-Vector3.UnitY, Rotation), Vector3.UnitY);
            return lightView * lightProjection;
        }
    }
}