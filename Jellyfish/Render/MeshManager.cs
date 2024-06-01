using System.Collections.Generic;
using Jellyfish.Audio;

namespace Jellyfish.Render;

public static class MeshManager
{
    private static readonly List<Mesh> meshes = new();

    public static void AddMesh(Mesh mesh)
    {
        meshes.Add(mesh);

        if (!mesh.IsDev)
            AudioManager.AddMesh(mesh.MeshPart);
    }

    public static void Draw(bool drawDev = true, Shader? shaderToUse = null)
    {
        foreach (var mesh in meshes)
        {
            if (mesh.IsDev && !drawDev)
                continue;

            if (mesh.ShouldDraw)
                mesh.Draw(shaderToUse);
        }
    }

    public static void Unload()
    {
        foreach (var mesh in meshes)
            mesh.Unload();
    }
}