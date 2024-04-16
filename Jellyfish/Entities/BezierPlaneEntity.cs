using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("plane_bezier")]
public class BezierPlaneEntity : BaseEntity
{
    private readonly BezierPlane _plane;

    public Vector2 Size { get; set; } = new(40, 20);
    public int Resolution { get; set; } = 2;

    public BezierPlaneEntity()
    {
        _plane = new BezierPlane(Size, Resolution);
    }

    public override void Load()
    {
        MeshManager.AddMesh(_plane);
        base.Load();
    }

    public override void Think()
    {
        _plane.Position = Position;
        _plane.Rotation = Rotation;

        base.Think();
    }
}