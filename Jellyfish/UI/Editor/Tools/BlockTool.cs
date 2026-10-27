using System;
using System.Collections.Generic;
using System.Linq;
using Hexa.NET.ImGui;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Render;
using Jellyfish.Utils;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.UI.Editor.Tools;

public class BlockTool : IEditorTool
{
    private enum BlockCreationStage
    {
        None,
        BaseSize,
        Height
    }

    private BlockCreationStage _blockStage;
    private int _blockAxis;
    private float _blockDirection;
    private Vector3 _blockStart;
    private Vector3 _blockEnd;
    private float _blockHeight;
    private float _blockBevel;
    private int _blockBevelSegments = 4;

    private Ray? _mouseRay;

    public EditorToolType Type => EditorToolType.Block;
    public string Name => "Block";
    public bool Selected { get; set; }

    public void DrawToolbar(EditorToolType? selectedTool, Action selectToolAction)
    {
        if (ImGui.RadioButton(Name, selectedTool == Type))
            selectToolAction();
    }

    public void DrawSidebar()
    {
        ImGui.SetNextItemWidth(60);
        ImGui.DragFloat("Bevel", ref _blockBevel, 0.25f, 0.0f, 1024.0f);
        ImGui.SetNextItemWidth(60);
        ImGui.DragInt("Segments", ref _blockBevelSegments, 0.1f, 1, 16);
    }

    public void Draw()
    {
        if (!Selected)
            return;

        Vector3 center;
        int axis;

        if (_blockStage == 0)
        {
            if (_mouseRay == null || !Editor.FindBlockSurface(_mouseRay.Value, out var point, out axis, out _))
                return;

            center = Editor.SnapToGrid(point, axis);
        }
        else
        {
            center = _blockEnd;
            axis = _blockAxis;
        }

        // grid around the cursor on the plane we're drawing on
        const int cells = 10;
        var gridSize = ConVarStorage.Get<int>("edt_gridsize");
        var u = (axis + 1) % 3;
        var v = (axis + 2) % 3;

        for (var i = -cells; i <= cells; i++)
        {
            var uStart = center;
            uStart[u] += i * gridSize;
            uStart[v] -= cells * gridSize;
            var uEnd = uStart;
            uEnd[v] += cells * gridSize * 2;

            var vStart = center;
            vStart[v] += i * gridSize;
            vStart[u] -= cells * gridSize;
            var vEnd = vStart;
            vEnd[u] += cells * gridSize * 2;

            var majorLine = gridSize * 8;
            DebugRender.DrawLine(uStart, uEnd, MathF.Abs(uStart[u] % majorLine) < 0.01f ? Editor.grid_major_color : Editor.grid_color);
            DebugRender.DrawLine(vStart, vEnd, MathF.Abs(vStart[v] % majorLine) < 0.01f ? Editor.grid_major_color : Editor.grid_color);
        }

        if (_blockStage == 0)
        {
            var uOffset = Vector3.Zero;
            uOffset[u] = gridSize / 2.0f;
            var vOffset = Vector3.Zero;
            vOffset[v] = gridSize / 2.0f;

            DebugRender.DrawLine(center - uOffset, center + uOffset);
            DebugRender.DrawLine(center - vOffset, center + vOffset);
            return;
        }

        var height = Vector3.Zero;
        if (_blockStage == BlockCreationStage.Height)
            height[_blockAxis] = _blockDirection * _blockHeight;

        var min = Vector3.ComponentMin(_blockStart, _blockEnd + height);
        var max = Vector3.ComponentMax(_blockStart, _blockEnd + height);
        var size = max - min;

        DebugRender.DrawBoundingBox(Vector3.Zero, new BoundingBox(max, min));
        DebugRender.DrawText(_blockEnd + height, $"{size.X} x {size.Y} x {size.Z}");
    }

    public void Input(KeyboardState keyboardState, MouseState mouseState, Ray ray, bool control, bool alt)
    {
        if (!Selected)
            return;

        var gridSize = ConVarStorage.Get<int>("edt_gridsize");

        _mouseRay = ray;

        // right click cancels the block that's being drawn
        if (_blockStage != 0 && mouseState.IsButtonPressed(MouseButton.Right))
            _blockStage = 0;

        if (_blockStage == BlockCreationStage.None)
        {
            if (mouseState.IsButtonPressed(MouseButton.Left) && Editor.FindBlockSurface(ray, out var point, out _blockAxis, out _blockDirection))
            {
                _blockStart = Editor.SnapToGrid(point, _blockAxis);
                _blockEnd = _blockStart;
                _blockStage = BlockCreationStage.BaseSize;
            }
        }
        else if (_blockStage == BlockCreationStage.BaseSize)
        {
            // keep the base on the plane it was started on
            var t = (_blockStart[_blockAxis] - ray.Origin[_blockAxis]) / ray.Direction[_blockAxis];
            if (MathF.Abs(ray.Direction[_blockAxis]) > 1e-6f && t > 0)
                _blockEnd = Editor.SnapToGrid(ray.Origin + ray.Direction * t, _blockAxis);

            if (!mouseState.IsButtonDown(MouseButton.Left))
            {
                var size = _blockEnd - _blockStart;
                var hasArea = Enumerable.Range(0, 3).Count(i => i != _blockAxis && MathF.Abs(size[i]) > 0.01f) == 2;

                _blockStage = hasArea ? BlockCreationStage.Height : BlockCreationStage.None;
                _blockHeight = gridSize;
            }
        }
        else if (_blockStage == BlockCreationStage.Height)
        {
            // closest point between the mouse ray and the line going up from the base corner
            var axis = Vector3.Zero;
            axis[_blockAxis] = _blockDirection;

            var b = Vector3.Dot(axis, ray.Direction);
            var denominator = 1.0f - b * b;
            if (denominator > 1e-4f)
            {
                var w = _blockEnd - ray.Origin;
                var height = (b * Vector3.Dot(ray.Direction, w) - Vector3.Dot(axis, w)) / denominator;

                if (ConVarStorage.Get<bool>("edt_snap"))
                    height = MathF.Round(height / gridSize) * gridSize;

                _blockHeight = height;
            }

            if (mouseState.IsButtonPressed(MouseButton.Left) && MathF.Abs(_blockHeight) > 0.01f)
            {
                CreateBlock();
                _blockStage = 0;
            }
        }
    }

    public void Reset()
    {
        _blockStage = 0;
    }

    private void CreateBlock()
    {
        var height = Vector3.Zero;
        height[_blockAxis] = _blockDirection * _blockHeight;

        var min = Vector3.ComponentMin(_blockStart, _blockEnd + height);
        var max = Vector3.ComponentMax(_blockStart, _blockEnd + height);
        var halfSize = (max - min) / 2;
        var radius = Math.Clamp(_blockBevel, 0f, Math.Min(halfSize.X, Math.Min(halfSize.Y, halfSize.Z)));
        var segments = Math.Max(1, _blockBevelSegments);
        var inner = halfSize - new Vector3(radius);

        var vertices = new List<Vector3>();
        var vertexIndices = new Dictionary<Vector3, int>();
        var faces = new List<EditableFace>();

        // each side is a grid of quads, with a bevel the outer rows get wrapped around the edges (same as the box entity)
        for (var axis = 0; axis < 3; axis++)
        {
            foreach (var sign in new[] { -1, 1 })
            {
                // u x v has to point outwards so faces end up counter-clockwise
                var uAxis = sign > 0 ? (axis + 1) % 3 : (axis + 2) % 3;
                var vAxis = sign > 0 ? (axis + 2) % 3 : (axis + 1) % 3;

                var uSamples = WorldMesh.GetBevelSamples(halfSize[uAxis], radius, segments);
                var vSamples = WorldMesh.GetBevelSamples(halfSize[vAxis], radius, segments);

                var grid = new int[uSamples.Count, vSamples.Count];
                for (var u = 0; u < uSamples.Count; u++)
                {
                    for (var v = 0; v < vSamples.Count; v++)
                    {
                        var flatPoint = Vector3.Zero;
                        flatPoint[axis] = sign * halfSize[axis];
                        flatPoint[uAxis] = uSamples[u];
                        flatPoint[vAxis] = vSamples[v];

                        var innerPoint = Vector3.Clamp(flatPoint, -inner, inner);
                        var position = radius > 0 ? innerPoint + (flatPoint - innerPoint).Normalized() * radius : flatPoint;

                        // share vertices between sides
                        var key = new Vector3(MathF.Round(position.X, 3), MathF.Round(position.Y, 3), MathF.Round(position.Z, 3));
                        if (!vertexIndices.TryGetValue(key, out var index))
                        {
                            index = vertices.Count;
                            vertexIndices.Add(key, index);
                            vertices.Add(position);
                        }

                        grid[u, v] = index;
                    }
                }

                for (var u = 0; u < uSamples.Count - 1; u++)
                {
                    for (var v = 0; v < vSamples.Count - 1; v++)
                    {
                        faces.Add(new EditableFace
                        {
                            Indices = [grid[u, v], grid[u + 1, v], grid[u + 1, v + 1], grid[u, v + 1]],
                            Material = MaterialBrowser.SelectedMaterial
                        });
                    }
                }
            }
        }

        var entity = Engine.EntityManager.CreateEntity("world_mesh");
        if (entity == null)
            return;

        entity.SetPropertyValue("Position", (min + max) / 2);
        entity.SetPropertyValue("Mesh", new EditableMesh
        {
            Vertices = vertices.ToArray(),
            Faces = faces.ToArray()
        });
        entity.Load();
    }
}