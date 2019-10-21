
using Jellyfish.Render;

namespace Jellyfish.Entities
{
    public class BezierPlaneEntity : BaseEntity
    {
        private BezierPlane plane;

        public BezierPlaneEntity()
        {
            plane = new BezierPlane();
            Load();
        }
        public override void Load()
        {
            MeshManager.AddMesh(plane);
            base.Load();
        }
    }
}
