using System.Collections.Generic;
using Jellyfish.Render;
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("plane_flat")]
public class Plane : BaseEntity
{
    private Mesh? _plane;
    public Vector2 Size { get; set; } = new(20, 20);

    public override void Load()
    {
        var a = new Vector3(-Size.X / 2.0f, Size.Y / 2.0f, 0);
        var b = new Vector3(Size.X / 2.0f, Size.Y / 2.0f, 0);
        var c = new Vector3(Size.X / 2.0f, -Size.Y / 2.0f, 0);
        var d = new Vector3(-Size.X / 2.0f, -Size.Y / 2.0f, 0);

        _plane = new Mesh(new MeshInfo
        {
            Name = "plane_flat",
            Vertices = new List<Vector3> { a, b, c, d, a, c },
            Texture = "test.png"
        });
        _plane.AddShader(new Main("test.png"));

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