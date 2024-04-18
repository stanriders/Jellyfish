using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("plane_bezier")]
public class BezierPlaneEntity : BaseEntity
{
    private BezierPlane? _plane;

    public Vector2 Size { get; set; } = new(40, 20);
    public int Resolution { get; set; } = 2;

    public override void Load()
    {
        _plane = new BezierPlane(Size, Resolution);
        MeshManager.AddMesh(_plane);
        base.Load();
    }

    public override void Think()
    {
        if (_plane != null)
        {
            _plane.Position = Position;
            _plane.Rotation = Rotation;
        }

        base.Think();
    }
}