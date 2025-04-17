using System.Collections.Generic;
using System.Linq;
using Jellyfish.Audio;
using Jellyfish.Utils;

namespace Jellyfish.Render;

public static class MeshManager
{
    private static readonly List<Mesh> meshes = new();
    private static readonly List<(Mesh, List<Vertex>)> updateQueue = new();

    public static BoundingBox SceneBoundingBox { get; private set; }

    private static bool drawing;

    public static void AddMesh(Mesh mesh)
    {
        mesh.Load();
        meshes.Add(mesh);

        SceneBoundingBox = new BoundingBox([SceneBoundingBox, mesh.BoundingBox]);

        if (!mesh.IsDev)
            AudioManager.AddMesh(mesh);
    }

    public static void RemoveMesh(Mesh mesh)
    {
        while (drawing)
        {
            // never remove meshes mid-drawing
        }

        meshes.Remove(mesh);
        mesh.Unload();

        // sounds expensive?
        SceneBoundingBox = new BoundingBox(meshes.Select(x => x.BoundingBox).ToArray());
    }

    public static void UpdateMesh(Mesh mesh, List<Vertex> vertices)
    {
        if (!drawing)
        {
            if (!updateQueue.Any(x=> x.Item1 == mesh))
                updateQueue.Add((mesh, vertices));
        }
    }

    public static void Draw(bool drawDev = true, Shader? shaderToUse = null, Frustum? frustum = null)
    {
        drawing = true;

        var playerPosition = Camera.Instance.Position;

        var sortedMeshes = meshes.OrderByDescending(x => !(x.Material?.AlphaTest ?? false))
            .ThenByDescending(x => (x.Position + x.BoundingBox.Center - playerPosition).Length);

        foreach (var mesh in sortedMeshes)
        {
            if (mesh.IsDev && !drawDev)
                continue;

            var frustumToUse = frustum ?? Camera.Instance.GetFrustum();
            if (mesh.ShouldDraw && frustumToUse.IsInside(mesh.Position, mesh.BoundingBox.Length))
                mesh.Draw(shaderToUse);
        }

        // ensures that all VBO updates happen post-rendering
        foreach (var update in updateQueue)
        {
            update.Item1.Update(update.Item2);
        }

        updateQueue.Clear();

        drawing = false;
    }

    public static void DrawGBuffer(bool drawDev = true)
    {
        drawing = true;

        foreach (var mesh in meshes)
        {
            if (mesh.IsDev && !drawDev)
                continue;

            if (mesh.ShouldDraw && Camera.Instance.GetFrustum().IsInside(mesh.Position, mesh.BoundingBox.Length))
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