
using System.Collections.Generic;
using Jellyfish.Render;
using OpenTK;

namespace Jellyfish.Entities
{
    class Plane : BaseEntity
    {
        private Mesh plane;

        public Plane(Vector3 c1, Vector3 c2, Vector3 c3, Vector3 c4)
        {
            plane = new Mesh(new MeshInfo
            { 
                Vertices = new List<Vector3>() { c1, c2, c3, c1, c4, c3 }
            });
            Load();
        }
        public override void Load()
        {
            MeshManager.AddMesh(plane);
            base.Load();
        }
    }
}
