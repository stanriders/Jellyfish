using System.Collections.Generic;
using Jellyfish.Audio;

namespace Jellyfish.Render;

public static class MeshManager
{
    private static readonly List<Mesh> meshes = new();

    public static void AddMesh(Mesh mesh)
    {
        meshes.Add(mesh);
        AudioManager.AddMesh(mesh.MeshPart);
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