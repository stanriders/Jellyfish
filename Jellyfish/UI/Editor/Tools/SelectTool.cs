using System;
using Hexa.NET.ImGui;
using Jellyfish.Console;
using Jellyfish.Render;
using Jellyfish.Utils;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.UI.Editor.Tools;

public class EditorEnableSelectToolHover() : ConVar<bool>("edt_tools_select_hover", true);

public class SelectTool : IEditorTool
{
    public EditorToolType Type => EditorToolType.Select;
    public string Name => "Select";
    public bool Selected { get; set; }

    private Ray? _mouseRay;

    private const uint entity_hover_color = 0x77DDEEFF;

    public void DrawToolbar(EditorToolType? selectedTool, Action selectToolAction)
    {
        if (ImGui.RadioButton(Name, selectedTool == Type))
            selectToolAction();
    }

    public void DrawSidebar()
    {
    }

    public void Draw()
    {
        if (!Selected)
            return;

        if (ConVarStorage.Get<bool>("edt_tools_select_hover") && _mouseRay != null)
        {
            var entity = Trace.IntersectsEntity(_mouseRay.Value);
            if (entity != null && entity.BoundingBox != null)
            {
                var position = entity.GetPropertyValue<Vector3>("Position");
                DebugRender.DrawText(position, entity.Name ?? "?", entity_hover_color);
                DebugRender.DrawBoundingBox(position, entity.BoundingBox.Value, entity_hover_color);
            }
        }
    }

    public void Input(KeyboardState keyboardState, MouseState mouseState, Ray ray, bool control, bool alt)
    {
        if (!Selected)
            return;

        _mouseRay = ray;

        if (mouseState.IsButtonPressed(MouseButton.Left))
        {
            var entity = Trace.IntersectsEntity(ray);
            if (entity == Editor.Instance.SelectedEntity)
                Editor.Instance.SelectedEntity = null;
            else
                Editor.Instance.SelectedEntity = entity;
        }

        if (Editor.Instance.SelectedEntity != null)
        {
            if (keyboardState.IsKeyPressed(Keys.Delete))
            {
                Engine.EntityManager.KillEntity(Editor.Instance.SelectedEntity);
                Editor.Instance.SelectedEntity = null;
            }
        }
    }

    public void Reset()
    {
    }
}