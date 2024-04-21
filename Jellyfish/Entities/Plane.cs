using System.Collections.Generic;
using Jellyfish.Render;
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Entities;

[Entity("plane_flat")]
public class Plane : BaseEntity
{
    private Mesh? _plane;

    public override IReadOnlyList<EntityProperty> EntityProperties { get; } = new List<EntityProperty>
    {
        new EntityProperty<Vector2>("Size",  new Vector2(20, 20)),
        new EntityProperty<string>("Texture", "test.png")
    };

    public override void Load()
    {
        var size = GetPropertyValue<Vector2>("Size");
        var textureProperty = GetPropertyValue<string>("Texture");
        if (textureProperty == null)
        {
            Log.Error("[Plane] Texture not set!");
            return;
        }

        var a = new Vector3(-size.X / 2.0f, size.Y / 2.0f, 0);
        var b = new Vector3(size.X / 2.0f, size.Y / 2.0f, 0);
        var c = new Vector3(size.X / 2.0f, -size.Y / 2.0f, 0);
        var d = new Vector3(-size.X / 2.0f, -size.Y / 2.0f, 0);

        Vector3 u = b - a;
        Vector3 v = c - b;
        Vector3 normal = Vector3.Cross(u, v).Normalized();

        var texture = $"materials/{textureProperty}";

        _plane = new Mesh(new MeshInfo
        {
            Name = "plane_flat",
            Vertices = new List<Vector3> { a, b, c, d, a, c },
            Normals = new List<Vector3> { normal, normal, normal, normal, normal, normal },
            UVs = new List<Vector2> { new(0, 0), new(1, 0), new(1, 1), new(0, 1), new(0, 0), new(1, 1) },
            Texture = texture
        });
        _plane.AddShader(new Main(texture));

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