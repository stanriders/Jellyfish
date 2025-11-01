using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jellyfish.Debug;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class MeshManager
{
    private readonly List<Mesh> _meshes = new();
    private readonly List<Mesh> _singleFrameMeshes = new();
    private readonly List<(Mesh, List<Vertex>)> _updateQueue = new();

    public IReadOnlyList<Mesh> Meshes => _meshes.AsReadOnly();
    public BoundingBox SceneBoundingBox { get; private set; }

    private bool _drawing;

    public void AddMesh(Mesh mesh, bool singleFrame = false)
    {
        mesh.Load();
        _meshes.Add(mesh);
        if (singleFrame)
            _singleFrameMeshes.Add(mesh);

        SceneBoundingBox = new BoundingBox([SceneBoundingBox, mesh.BoundingBox]);
    }

    public void RemoveMesh(Mesh mesh)
    {
        while (_drawing)
        {
            // never remove meshes mid-drawing
        }

        _meshes.Remove(mesh);
        mesh.Unload();

        // sounds expensive?
        SceneBoundingBox = new BoundingBox(_meshes.Select(x => x.BoundingBox).ToArray());
    }

    public void UpdateMesh(Mesh mesh, List<Vertex> vertices)
    {
        if (!_drawing)
        {
            if (!_updateQueue.Any(x=> x.Item1 == mesh))
                _updateQueue.Add((mesh, vertices));
        }
    }

    public void Draw(bool drawDev = true, Shader? shaderToUse = null, Frustum? frustum = null)
    {
        _drawing = true;
        var drawStopwatch = Stopwatch.StartNew();

        var sortingPosition = frustum?.NearPlaneCenter ?? Engine.MainViewport.Position;

        var opaqueObjects = _meshes.Where(x => !(x.Material?.GetParam<bool>("AlphaTest") ?? false)).ToArray();
        var transluscentObjects = _meshes.Where(x => !opaqueObjects.Contains(x))
            .OrderByDescending(x => ((x.Position + x.BoundingBox.Center) - sortingPosition).Length)
            .ToArray();

        var stopwatch = Stopwatch.StartNew();
        foreach (var mesh in opaqueObjects)
            DrawMesh(mesh, drawDev, shaderToUse, frustum);
        PerformanceMeasurment.Add("MeshManager.Draw.Opaque", stopwatch.Elapsed.TotalMilliseconds);

        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BlendEquation(BlendEquationMode.FuncAdd);

        stopwatch.Restart();
        foreach (var mesh in transluscentObjects)
            DrawMesh(mesh, drawDev, shaderToUse, frustum);
        PerformanceMeasurment.Add("MeshManager.Draw.Transluscent", stopwatch.Elapsed.TotalMilliseconds);

        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);

        // ensures that all VBO updates happen post-rendering
        foreach (var update in _updateQueue)
        {
            update.Item1.Update(update.Item2);
        }

        _updateQueue.Clear();

        _drawing = false;

        if (drawDev)
        {
            foreach (var singleFrameMesh in _singleFrameMeshes)
            {
                RemoveMesh(singleFrameMesh);
            }

            _singleFrameMeshes.Clear();
        }
        frustum?.Dispose();
        PerformanceMeasurment.Add("MeshManager.Draw", drawStopwatch.Elapsed.TotalMilliseconds);
    }

    public void DrawGBuffer(bool drawDev = true)
    {
        _drawing = true;
        var drawStopwatch = Stopwatch.StartNew();

        using var playerFrustum = Engine.MainViewport.GetFrustum();
        var playerPosition = playerFrustum.NearPlaneCenter;
        var opaqueObjects = _meshes.Where(x => !(x.Material?.GetParam<bool>("AlphaTest") ?? false)).ToArray();
        var transluscentObjects = _meshes.Where(x => !opaqueObjects.Contains(x))
            .OrderByDescending(x => ((x.Position + x.BoundingBox.Center) - playerPosition).Length)
            .ToArray();

        foreach (var mesh in opaqueObjects)
        {
            if (mesh.IsDev && !drawDev)
                continue;

            if (mesh.ShouldDraw && playerFrustum.IsInside(mesh.Position + mesh.BoundingBox.Center, mesh.BoundingBox.Length))
                mesh.DrawGBuffer();
        }

        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BlendEquation(BlendEquationMode.FuncAdd);

        foreach (var mesh in transluscentObjects)
        {
            if (mesh.IsDev && !drawDev)
                continue;

            if (mesh.ShouldDraw && playerFrustum.IsInside(mesh.Position + mesh.BoundingBox.Center, mesh.BoundingBox.Length))
                mesh.DrawGBuffer();
        }

        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);

        PerformanceMeasurment.Add("MeshManager.DrawGBuffer", drawStopwatch.Elapsed.TotalMilliseconds);
        _drawing = false;
    }

    private void DrawMesh(Mesh mesh, bool drawDev = true, Shader? shaderToUse = null, Frustum? frustum = null)
    {
        if (mesh.IsDev && !drawDev)
            return;

        if (mesh.ShouldDraw)
        {
            if (frustum != null && !frustum.Value.IsInside(mesh.Position + mesh.BoundingBox.Center, mesh.BoundingBox.Length))
                return;

            mesh.Draw(shaderToUse);
        }
    }

    public void Unload()
    {
        foreach (var mesh in _meshes)
            mesh.Unload();
    }
}