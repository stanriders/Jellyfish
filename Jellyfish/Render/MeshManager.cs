using System.Collections.Generic;

namespace Jellyfish.Render;

public static class MeshManager
{
    private static readonly List<Mesh> meshes = new();

    public static void AddMesh(Mesh mesh)
    {
        meshes.Add(mesh);
    }

    public static void Draw()
    {
        foreach (var mesh in meshes)
            mesh.Draw();
    }

    public static void Unload()
    {
        foreach (var mesh in meshes)
            mesh.Unload();
    }
}