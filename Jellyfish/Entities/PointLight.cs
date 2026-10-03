using Jellyfish.Utils;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_point")]
public class PointLight : LightEntity
{
    public override bool DrawDevCone { get; set; } = true;

    public PointLight()
    {
        AddProperty("Quadratic", 0.8f);
        AddProperty("Linear", 0.15f);
        AddProperty("Constant", 0.05f);
        AddProperty("FarPlane", 500f);
    }

    private const int projections = 6;

    private Matrix4?[] _projectionsCache = new Matrix4?[projections];

    public override float NearPlane => 0.1f;
    public override float FarPlane => GetPropertyValue<float>("FarPlane");
    public override int ShadowResolution => 1024;
    public override int ProjectionCount => projections;

    public override Matrix4 Projection(int index)
    {
        if (_projectionsCache[index] != null)
        {
            return _projectionsCache[index]!.Value;
        }

        var (dir, up) = CommonShapes.CubeFaces[index];
        var lightProjection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90f), 1.0f, NearPlane, FarPlane);
        var lightView = Matrix4.LookAt(Position, Position + dir, up);

        var finalProjection = lightView * lightProjection;

        _projectionsCache[index] = finalProjection;

        return finalProjection;
    }

    public override void ClearProjectionCache()
    {
        _projectionsCache = new Matrix4?[projections];
    }
}