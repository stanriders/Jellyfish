using System.Collections.Generic;
using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

public class Plane : BaseEntity
{
    private readonly Mesh _plane;

    public Plane(Vector3 c1, Vector3 c2, Vector3 c3, Vector3 c4)
    {
        _plane = new Mesh(new MeshInfo
        {
            Vertices = new List<Vector3> { c1, c2, c3, c1, c4, c3 }
        });
        Load();
    }

    public override void Load()
    {
        MeshManager.AddMesh(_plane);
        base.Load();
    }
}