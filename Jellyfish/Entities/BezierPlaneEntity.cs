using Jellyfish.Render;

namespace Jellyfish.Entities;

[Entity("plane_bezier")]
public class BezierPlaneEntity : BaseEntity
{
    private readonly BezierPlane _plane;

    public BezierPlaneEntity()
    {
        _plane = new BezierPlane();
    }

    public override void Load()
    {
        MeshManager.AddMesh(_plane);
        base.Load();
    }
}