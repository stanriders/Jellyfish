using System.Collections.Generic;

namespace Jellyfish.Render
{
    static class MeshManager
    {
        private static readonly List<Mesh> meshes = new List<Mesh>();

        public static void AddMesh(Mesh mesh)
        {
            meshes.Add(mesh);
        }

        public static void Draw()
        {
            foreach (var mesh in meshes)
            {
                mesh.Draw();
            }
        }

        public static void Unload()
        {
            foreach (var mesh in meshes)
            {
                mesh.Unload();
            }
        }
    }
}
