using Jellyfish.Render;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Entities;

[Entity("plane_bezier")]
public class BezierPlaneEntity : BaseEntity
{
    private BezierPlane? _plane;

    public BezierPlaneEntity()
    {
        AddProperty("Size", new Vector2(20, 20));
        AddProperty("QuadSize", 2);
        AddProperty("Texture", "test.png");
    }

    public override void Load()
    {
        var texture = GetPropertyValue<string>("Texture");
        if (texture == null)
        {
            Log.Error("[BezierPlaneEntity] Texture not set!");
            return;
        }

        _plane = new BezierPlane(GetPropertyValue<Vector2>("Size"), texture, GetPropertyValue<int>("QuadSize"));
        MeshManager.AddMesh(_plane);
        base.Load();
    }

    public override void Think()
    {
        if (_plane != null)
        {
            _plane.Position = GetPropertyValue<Vector3>("Position");
            _plane.Rotation = GetPropertyValue<Vector3>("Rotation");
        }

        base.Think();
    }
}