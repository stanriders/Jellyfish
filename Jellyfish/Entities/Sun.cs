using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_sun")]
public class Sun : LightEntity
{
    public override float NearPlane => 1f;
    public override float FarPlane => Position.Y * 1.5f;
    public override int ShadowResolution => 4096;

    public override Matrix4[] Projections
    {
        get
        {
            var position = Camera.Instance.Position + Vector3.Transform(GetPropertyValue<Vector3>("Position"), Rotation);
            
            var lightProjection = Matrix4.CreateOrthographic(position.Y * 1.5f, position.Y * 1.5f, NearPlane, FarPlane);
            var lightView = Matrix4.LookAt(position, position + Vector3.Transform(-Vector3.UnitY, Rotation), Vector3.UnitY);
            return [lightView * lightProjection];
        }
    }
}