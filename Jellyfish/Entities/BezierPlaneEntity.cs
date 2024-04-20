using Jellyfish.Render;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Jellyfish.Entities;

[Entity("plane_bezier")]
public class BezierPlaneEntity : BaseEntity
{
    private BezierPlane? _plane;

    public override IReadOnlyList<EntityProperty> EntityProperties { get; } = new List<EntityProperty>
    {
        new EntityProperty<Vector2>("Size", new Vector2(20,20)),
        new EntityProperty<int>("Resolution", 2)
    };

    public override void Load()
    {
        _plane = new BezierPlane(GetPropertyValue<Vector2>("Size"), GetPropertyValue<int>("Resolution"));
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