using System.Collections.Generic;
using System.Linq;
using Jellyfish.Audio;
using Jellyfish.Entities;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public static class MeshManager
{
    private static readonly List<Mesh> meshes = new();
    private static readonly List<(Mesh, MeshPart)> updateQueue = new();

    private static bool drawing;

    public static void AddMesh(Mesh mesh)
    {
        meshes.Add(mesh);

        if (!mesh.IsDev)
            AudioManager.AddMesh(mesh.MeshPart);
    }

    public static void RemoveMesh(Mesh mesh)
    {
        while (drawing)
        {
            // never remove meshes mid-drawing
        }

        meshes.Remove(mesh);
        mesh.Unload();
    }

    public static void UpdateMesh(Mesh mesh, MeshPart part)
    {
        if (!drawing)
        {
            if (!updateQueue.Any(x=> x.Item1 == mesh))
                updateQueue.Add((mesh, part));
        }
    }

    public static void Draw(bool drawDev = true, Shader? shaderToUse = null)
    {
        drawing = true;

        var playerPosition = Player.Instance?.GetPropertyValue<Vector3>("Position");

        var sortedMeshes = meshes.OrderByDescending(x => !(x.Material?.AlphaTest ?? false))
            .ThenByDescending(x =>
                ((x.Position + x.MeshPart.BoundingBox.Center) - playerPosition)?
                .Length);

        foreach (var mesh in sortedMeshes)
        {
            if (mesh.IsDev && !drawDev)
                continue;

            if (mesh.ShouldDraw)
                mesh.Draw(shaderToUse);
        }

        // ensures that all VBO updates happen post-rendering
        foreach (var update in updateQueue)
        {
            update.Item1.SetMeshPart(update.Item2);
        }

        updateQueue.Clear();

        drawing = false;
    }

    public static void DrawGBuffer(bool drawDev = false)
    {
        drawing = true;

        foreach (var mesh in meshes)
        {
            if (mesh.IsDev && !drawDev)
                continue;

            if (mesh.ShouldDraw)
                mesh.DrawGBuffer();
        }

        drawing = false;
    }

    public static void Unload()
    {
        foreach (var mesh in meshes)
            mesh.Unload();
    }
}