using Jellyfish.Render;

namespace Jellyfish.Entities;

public class BezierPlaneEntity : BaseEntity
{
    private readonly BezierPlane _plane;

    public BezierPlaneEntity()
    {
        _plane = new BezierPlane();
        Load();
    }

    public override void Load()
    {
        MeshManager.AddMesh(_plane);
        base.Load();
    }
}