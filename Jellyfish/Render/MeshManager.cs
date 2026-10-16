using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public record MeshBuffers(VertexBuffer Vbo, IndexBuffer? Ibo, VertexArray Vao);

public class MeshManager
{
    private readonly List<Mesh> _opaqueMeshes = new();
    private readonly List<Mesh> _translucentMeshes = new();

    private readonly List<Mesh> _singleFrameMeshes = new();
    private readonly List<(Mesh, List<Vertex>)> _updateQueue = new();

    private readonly Dictionary<string, (MeshBuffers Buffers, int References)> _buffers = new();

    public IReadOnlyList<Mesh> Meshes => new ReadOnlyCollection<Mesh>([.._opaqueMeshes, .._translucentMeshes]);
    public BoundingBox SceneBoundingBox { get; private set; }

    public void AddMesh(Mesh mesh, bool singleFrame = false)
    {
        var sharedBuffersKey = mesh.Model?.SourcePath != null ? $"{mesh.Model.SourcePath}_{mesh.Name}" : null;
        if (sharedBuffersKey != null && _buffers.TryGetValue(sharedBuffersKey, out var shared))
        {
            mesh.Load(shared.Buffers);
            _buffers[sharedBuffersKey] = (shared.Buffers, shared.References + 1);
        }
        else
        {
            var buffers = mesh.Load();
            if (sharedBuffersKey != null)
                _buffers[sharedBuffersKey] = (buffers, 1);
        }

        if (mesh.Material?.GetParam<bool>("AlphaTest") ?? false)
        {
            _translucentMeshes.Add(mesh);
        }
        else
        {
            _opaqueMeshes.Add(mesh);
        }

        if (singleFrame)
            _singleFrameMeshes.Add(mesh);
        else
            UpdateSceneBoundingBox();

    }

    public void RemoveMesh(Mesh mesh)
    {
        Scheduler.RenderSchedule(() =>
        {
            if (_translucentMeshes.Contains(mesh))
                _translucentMeshes.Remove(mesh);
            else
                _opaqueMeshes.Remove(mesh);

            var disposeBuffers = true;

            var sharedBuffersKey = mesh.Model?.SourcePath != null ? $"{mesh.Model.SourcePath}_{mesh.Name}" : null;
            if (sharedBuffersKey != null && _buffers.TryGetValue(sharedBuffersKey, out var shared))
            {
                if (shared.References <= 1)
                {
                    _buffers.Remove(sharedBuffersKey);
                    disposeBuffers = true;
                }
                else
                {
                    _buffers[sharedBuffersKey] = (shared.Buffers, shared.References - 1);
                    disposeBuffers = false;
                }
            }

            mesh.Unload(disposeBuffers);

            // sounds expensive?
            UpdateSceneBoundingBox();
        });
    }

    public void UpdateMesh(Mesh mesh, List<Vertex> vertices)
    {
        Scheduler.RenderSchedule(() =>
        {
            if (!_updateQueue.Any(x=> x.Item1 == mesh))
                _updateQueue.Add((mesh, vertices));
        });
    }

    public void DrawOpaque(bool drawDev = true, Shader? shaderToUse = null, Frustum? frustum = null, bool gBuffer = false)
    {
        using var _ = new PerformanceMeasure($"MeshManager.Draw.Opaque{(gBuffer ? ".GBuffer" : "")}");

        foreach (var mesh in _opaqueMeshes)
            DrawMesh(mesh, drawDev, shaderToUse, frustum, gBuffer);
    }

    public void DrawTranslucent(bool drawDev = true, Shader? shaderToUse = null, Frustum? frustum = null)
    {
        using var _ = new PerformanceMeasure("MeshManager.Draw.Translucent");
        if (_translucentMeshes.Count == 0)
            return;

        var sortingPosition = frustum?.NearPlaneCenter ?? Engine.MainViewport.Position;

        var transluscentObjects = _translucentMeshes
            .OrderByDescending(x => ((x.Position + x.BoundingBox.Center) - sortingPosition).Length)
            .ToArray();

        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.BlendEquation(BlendEquationMode.FuncAdd);

        foreach (var mesh in transluscentObjects)
            DrawMesh(mesh, drawDev, shaderToUse, frustum);

        GL.Disable(EnableCap.Blend);
        GL.DepthMask(true);
    }

    private void DrawMesh(Mesh mesh, bool drawDev = true, Shader? shaderToUse = null, Frustum? frustum = null, bool gBuffer = false)
    {
        if (mesh.IsDev && !drawDev)
            return;

        if (mesh.ShouldDraw)
        {
            var boundingBox = (mesh.Model?.BoundingBox ?? mesh.BoundingBox).Translate(Matrix4.CreateTranslation(mesh.Position));
            if (frustum != null && !frustum.Value.IsInside(boundingBox))
                return;

            // todo: this is UGLY and needs to be completely remade
            if (gBuffer)
                mesh.DrawGBuffer();
            else
                mesh.Draw(shaderToUse);
        }
    }

    private void UpdateMeshes()
    {
        foreach (var update in _updateQueue)
        {
            update.Item1.Update(update.Item2);
        }

        _updateQueue.Clear();
    }

    public void PostDraw(bool drawDev = true)
    {
        // ensure that all VBO updates happen post-rendering
        UpdateMeshes();

        if (drawDev) // kinda a hack: we need single frame meshes to actually survive to the end of the frame so we assume they're all dev meshes
        {
            foreach (var singleFrameMesh in _singleFrameMeshes)
            {
                RemoveMesh(singleFrameMesh);
            }

            _singleFrameMeshes.Clear();
        }
    }

    public void Unload()
    {
        foreach (var mesh in _opaqueMeshes)
            mesh.Unload(!mesh.UsesShaderBuffers);

        foreach (var mesh in _translucentMeshes)
            mesh.Unload(!mesh.UsesShaderBuffers);

        foreach (var (buffers, _) in _buffers.Values)
        {
            buffers.Vbo.Dispose();
            buffers.Ibo?.Dispose();
            buffers.Vao.Dispose();
        }

        _buffers.Clear();
    }

    public void UpdateSceneBoundingBox()
    {
        SceneBoundingBox = new BoundingBox(Meshes.Select(x => x.BoundingBox).ToArray());
    }
}