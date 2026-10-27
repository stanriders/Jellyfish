using System;
using Hexa.NET.ImGui;
using Jellyfish.Console;
using Jellyfish.Render;
using Jellyfish.Utils;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.UI.Editor.Tools;

public class EntityAddTool : IEditorTool
{
    public EditorToolType Type => EditorToolType.EntityAdd;
    public string Name => "Add Entity";
    public bool Selected { get; set; }

    private Ray? _mouseRay;
    private string? _selectedEntityType;

    public void DrawToolbar(EditorToolType? selectedTool, Action selectToolAction)
    {
        if (ImGui.BeginMenu("Add Entity"))
        {
            foreach (var entityClass in Engine.EntityManager.EntityClasses)
            {
                if (ImGui.MenuItem(entityClass, "", entityClass == _selectedEntityType))
                {
                    _selectedEntityType = entityClass;
                    selectToolAction();
                }
            }

            ImGui.EndMenu();
        }
    }

    public void DrawSidebar()
    {
    }

    public void Draw()
    {
        if (!Selected)
            return;

        Vector3 center;
        int axis;

        if (_mouseRay == null || !Editor.FindBlockSurface(_mouseRay.Value, out var point, out axis, out _))
            return;

        center = Editor.SnapToGrid(point, axis);

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
            DebugRender.DrawLine(uStart, uEnd,
                MathF.Abs(uStart[u] % majorLine) < 0.01f ? Editor.grid_major_color : Editor.grid_color);
            DebugRender.DrawLine(vStart, vEnd,
                MathF.Abs(vStart[v] % majorLine) < 0.01f ? Editor.grid_major_color : Editor.grid_color);
        }

        var uOffset = Vector3.Zero;
        uOffset[u] = gridSize / 2.0f;
        var vOffset = Vector3.Zero;
        vOffset[v] = gridSize / 2.0f;

        DebugRender.DrawLine(center - uOffset, center + uOffset);
        DebugRender.DrawLine(center - vOffset, center + vOffset);
    }

    public void Input(KeyboardState keyboardState, MouseState mouseState, Ray ray, bool control, bool alt)
    {
        if (!Selected)
            return;

        _mouseRay = ray;

        if (_selectedEntityType == null)
            return;

        if (mouseState.IsButtonPressed(MouseButton.Left) && Editor.FindBlockSurface(ray, out var point, out var blockAxis, out _))
        {
            var position = Editor.SnapToGrid(point, blockAxis);
            var entity = Engine.EntityManager.CreateEntity(_selectedEntityType);
            if (entity != null)
            {
                entity.SetPropertyValue("Position", position);
                Editor.Instance.SelectedEntity = entity;
            }

            _selectedEntityType = null;

            Editor.Instance.ResetTool();
        }
    }

    public void Reset()
    {
        _selectedEntityType = null;
    }
}