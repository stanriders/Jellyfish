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
        AddProperty("Resolution", 2);
        AddProperty("Texture", "test.png");
    }

    public override void Load()
    {
        var textureProperty = GetPropertyValue<string>("Texture");
        if (textureProperty == null)
        {
            Log.Error("[BezierPlaneEntity] Texture not set!");
            return;
        }

        _plane = new BezierPlane(GetPropertyValue<Vector2>("Size"), GetPropertyValue<int>("Resolution"), textureProperty);
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