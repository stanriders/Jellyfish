using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets.Dialogs;
using Hexa.NET.ImGuizmo;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Input;
using Jellyfish.Render;
using Jellyfish.UI.Components;
using Jellyfish.UI.Editor.Tools;
using Jellyfish.Utils;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Quaternion = OpenTK.Mathematics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace Jellyfish.UI.Editor;

public class EditorSnapToGrid() : ConVar<bool>("edt_snap", true);

public partial class Editor : IUiPanel, IInputHandler
{
    [ConVar("edt_enable",
#if DEBUG
    true)]
#else
    false)]
#endif
    private partial bool Enabled { get; set; }

    [ConVar("edt_gridsize", 16)]
    private partial int GridSize { get; set; }

    public BaseEntity? SelectedEntity { get; set; }

    private bool _usingGizmo = false;
    private static bool setUpDocking = true;

    private const float camera_speed = 120.0f;
    private const float sensitivity = 0.2f;

    // ImGui colors are ABGR
    public const uint grid_color = 0x30FFFFFF;
    public const uint grid_major_color = 0x80FFFFFF;

    private IEditorTool _tool;

    private readonly IEditorTool _defaultTool = new SelectTool();
    private readonly List<IEditorTool> _tools;

    public static Editor Instance = null!;

    public Editor()
    {
        Engine.InputManager.RegisterInputHandler(this);
        Instance = this;

        _tools =
        [
            _defaultTool,
            new BlockTool(),
            new FaceTool(),
            new EntityAddTool()
        ];

        _tool = _defaultTool;
        _tool.Selected = true;
    }

    public unsafe void Frame(double timeElapsed)
    {
        if (!Enabled)
            return;

        if (!Engine.Loaded)
            return;

        if (SelectedEntity?.MarkedForDeath ?? false)
        {
            SelectedEntity = null;
            _tool.Reset();
        }

        ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0.1f, 0.1f, 0.1f, 0.3f));

        var viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(Vector2.Zero);
        if (ImGui.Begin("Editor Top",
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoResize))
        {
            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.BeginMenu("Maps"))
                    {
                        foreach (var map in MapLoader.GetMapList())
                        {
                            if (ImGui.MenuItem(map))
                            {
                                Engine.QueuedMap = map;
                            }
                        }

                        ImGui.EndMenu();
                    }

                    ConVarComponents.MenuItemCommand("map_save", "Save", "Ctrl+S");

                    if (ImGui.MenuItem("Close"))
                    {
                        Engine.ShouldQuit = true;
                    }
                    ImGui.EndMenu();
                }

                if (ImGui.BeginMenu("Windows"))
                {
                    ConVarComponents.MenuItem("edt_texturelist", "Texture browser");
                    ConVarComponents.MenuItem("edt_meshbrowser", "Mesh browser");
                    ConVarComponents.MenuItem("edt_perfpanel", "Performance stats");
                    ImGui.EndMenu();
                }

                ConVarComponents.Button("mat_ibl_render", "Update light probes");

                ImGui.Separator();

                foreach (var tool in _tools)
                {
                    tool.DrawToolbar(_tool.Type, () => SetTool(tool));
                    ImGui.Separator();
                }

                ConVarComponents.Checkbox("edt_snap", "Snap");
                ImGui.Text($"Grid: {GridSize} ([ / ])");

                ImGui.EndMainMenuBar();
            }
        }
        ImGui.End();

        var editorDock = ImGui.GetID("EditorDock");

        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        if (ImGui.Begin("Dockable Editor", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar |
                                           ImGuiWindowFlags.NoResize |
                                           ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar |
                                           ImGuiWindowFlags.NoDecoration |
                                           ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoFocusOnAppearing))
        {
            ImGui.PopStyleVar();

            ImGui.DockSpace(editorDock, viewport.WorkSize,
                ImGuiDockNodeFlags.PassthruCentralNode);

            ImGui.SetNextWindowBgAlpha(0.5f);
            const string editorParamsName = "Editor params";
            if (ImGui.Begin(editorParamsName))
            {
                ConVarComponents.Checkbox("edt_drawcones", "Enable debug cones");
                ConVarComponents.Checkbox("edt_drawnames", "Show entity names");
                ConVarComponents.Checkbox("phys_debug", "Enable physics debug overlay");
                ConVarComponents.Checkbox("audio_debug", "Enable audio debug overlay");
            }

            ImGui.End();

            ImGui.SetNextWindowBgAlpha(0.5f);
            const string entityControlName = "Entity controls";
            if (ImGui.Begin("Entity controls"))
            {
                if (ImGui.BeginListBox("##Entity list", new Vector2(-1, 10 * ImGui.GetTextLineHeightWithSpacing())))
                {
                    foreach (var entity in Engine.EntityManager.Entities.OrderBy(x => x.Name).Where(x => x is not WorldMesh))
                    {
                        if (ImGui.MenuItem($"{entity.Name} ({entity.ClassName})", "", SelectedEntity?.Name == entity.Name))
                        {
                            SelectedEntity = entity;
                            _tool.Reset();
                        }
                    }

                    ImGui.EndListBox();
                }

                ImGui.Separator();

                if (SelectedEntity != null)
                {
                    ImGui.Text(SelectedEntity.GetType().ToString());
                    foreach (var entityProperty in SelectedEntity.EntityProperties)
                    {
                        AddProperty(SelectedEntity, entityProperty);
                    }

                    ImGui.Spacing();

                    foreach (var entityAction in SelectedEntity.EntityActions.OrderBy(x => x.Name))
                    {
                        if (ImGui.Button(entityAction.Name))
                            entityAction.Act();
                    }

                    ImGui.Spacing();

                    if (!SelectedEntity.Loaded)
                    {
                        if (ImGui.Button("Load"))
                        {
                            SelectedEntity.Load();
                        }
                    }
                }
            }

            ImGui.End();

            ImGui.SetNextWindowBgAlpha(0.5f);
            const string toolControlName = "Tool controls";
            if (ImGui.Begin("Tool controls"))
            {
                _tool.DrawSidebar();
            }
            ImGui.End();

            ImGui.SetNextWindowBgAlpha(0.5f);

            if (setUpDocking)
            {
                setUpDocking = false;

                var dockIdRight = ImGuiP.DockBuilderSplitNode(editorDock, ImGuiDir.Right, 0.15f, null, &editorDock);
                var dockIdRightTop = ImGuiP.DockBuilderSplitNode(dockIdRight, ImGuiDir.Up, 0.15f, null, &dockIdRight);
                var dockIdRightMiddle = ImGuiP.DockBuilderSplitNode(dockIdRight, ImGuiDir.Up, 0.55f, null, &dockIdRight);
                var dockIdRightBottom = ImGuiP.DockBuilderSplitNode(dockIdRight, ImGuiDir.Up, 0.3f, null, &dockIdRight);
                ImGuiP.DockBuilderDockWindow(editorParamsName, dockIdRightTop);
                ImGuiP.DockBuilderDockWindow(entityControlName, dockIdRightMiddle);
                ImGuiP.DockBuilderDockWindow(toolControlName, dockIdRightMiddle);
                ImGuiP.DockBuilderDockWindow("Materials", dockIdRightBottom);
                ImGuiP.DockBuilderFinish(dockIdRight);
                ImGuiP.DockBuilderFinish(editorDock);
            }
        }

        ImGui.End();
        ImGui.PopStyleColor();

        DrawSelectedEntityControls();

        _tool.Draw();
    }

    public void Unload()
    {
        SelectedEntity = null;
        _tool.Reset();
    }

    private unsafe void DrawSelectedEntityControls()
    {
        if (SelectedEntity == null)
            return;

        if (SelectedEntity.BoundingBox != null)
            DebugRender.DrawBoundingBox(SelectedEntity.GetPropertyValue<Vector3>("Position"),
                SelectedEntity.BoundingBox.Value);

        if (SelectedEntity is IHaveFrustum frustumEntity)
        {
            DebugRender.DrawFrustum(frustumEntity.GetFrustum());
        }

        DebugRender.DrawText(SelectedEntity.GetPropertyValue<Vector3>("Position") + new Vector3(0, 3, 0), SelectedEntity.Name ?? "null");

        _usingGizmo = false;

        // gizmos would get in the way of the other tools
        if (_tool.Type != EditorToolType.Select)
            return;

        fixed (float* view = Engine.MainViewport.GetViewMatrix().ToFloatArray())
        fixed (float* proj = Engine.MainViewport.GetProjectionMatrix().ToFloatArray())
        fixed (float* snap = new[] { 0.1f, 0.1f, 0.1f })
        {
            ImGuizmo.SetID(SelectedEntity.GetHashCode());

            var hasScale = SelectedEntity.CanEditProperty("Scale");
            var hasSize = SelectedEntity.CanEditProperty("Size");
            var hasRotation = SelectedEntity.CanEditProperty("Rotation");

            var sizeType = SelectedEntity.EntityProperties.SingleOrDefault(x => x.Name == "Size")?.Type;

            var sizeValue = Vector3.One;
            if (sizeType == typeof(Vector3))
                sizeValue = SelectedEntity.GetPropertyValue<Vector3>("Size");
            else if (sizeType == typeof(OpenTK.Mathematics.Vector2))
                sizeValue = new Vector3(SelectedEntity.GetPropertyValue<OpenTK.Mathematics.Vector2>("Size"), 1f);

            var entityRotation = hasRotation
                ? Matrix4.CreateFromQuaternion(SelectedEntity.GetPropertyValue<Quaternion>("Rotation"))
                : Matrix4.Identity;

            var entityScale = hasSize
                ? Matrix4.CreateScale(sizeValue)
                : hasScale
                    ? Matrix4.CreateScale(SelectedEntity.GetPropertyValue<Vector3>("Scale"))
                    : Matrix4.Identity;

            var entityTransform = entityScale * entityRotation *
                                  Matrix4.CreateTranslation(
                                      SelectedEntity.GetPropertyValue<Vector3>("Position"));
            var transformArray = entityTransform.ToFloatArray();

            fixed (float* transformArrayPinned = transformArray)
            {
                ImGuizmo.Enable(true);

                var operations = ImGuizmoOperation.Translate;
                if (hasRotation)
                    operations |= ImGuizmoOperation.Rotate;
                if (hasScale)
                    operations |= ImGuizmoOperation.Scale;

                ImGuizmo.SetID(0);
                if (ImGuizmo.Manipulate(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                        operations, ImGuizmoMode.Local,
                        ref Unsafe.AsRef<float>(transformArrayPinned),
                        null,
                        ref Unsafe.AsRef<float>(snap)))
                {
                    _usingGizmo = true;
                    SelectedEntity.SetPropertyValue("Position",
                        transformArray.ToMatrix().ExtractTranslation());

                    if (hasRotation)
                    {
                        SelectedEntity.SetPropertyValue("Rotation",
                            transformArray.ToMatrix().ExtractRotation());
                    }

                    if (hasSize)
                    {
                        var newSize = transformArray.ToMatrix().ExtractScale();
                        if (sizeType == typeof(Vector3))
                            SelectedEntity.SetPropertyValue("Size", newSize);
                        else if (sizeType == typeof(OpenTK.Mathematics.Vector2))
                            SelectedEntity.SetPropertyValue("Size", new OpenTK.Mathematics.Vector2(newSize.X, newSize.Y));
                    }
                    else if (hasScale)
                    {
                        SelectedEntity.SetPropertyValue("Scale", transformArray.ToMatrix().ExtractScale());
                    }

                    if (SelectedEntity is IPhysicsEntity physicsEntity)
                    {
                        physicsEntity.ResetVelocity();
                    }
                }
                else
                {
                    if (_usingGizmo && SelectedEntity is IPhysicsEntity physicsEntity)
                    {
                        physicsEntity.ResetVelocity();
                    }
                }
            }

            foreach (var gizmoProperty in SelectedEntity.EntityProperties.Where(x => x.ShowGizmo))
            {
                if (gizmoProperty.Value is Vector3[] arr)
                {
                    for (var i = 0; i < arr.Length; i++)
                    {
                        var point = arr[i];
                        var propertyTransform = (Matrix4.CreateTranslation(point) * entityTransform)
                            .ToFloatArray();

                        fixed (float* propertyTransformArrayPinned = propertyTransform)
                        {
                            ImGuizmo.SetID(SelectedEntity.GetHashCode() + gizmoProperty.GetHashCode() + i);

                            ImGuizmo.DrawCubes(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                                ref Unsafe.AsRef<float>(propertyTransformArrayPinned), 1);

                            if (ImGuizmo.Manipulate(ref Unsafe.AsRef<float>(view),
                                    ref Unsafe.AsRef<float>(proj),
                                    ImGuizmoOperation.Translate, ImGuizmoMode.World,
                                    ref Unsafe.AsRef<float>(propertyTransformArrayPinned),
                                    null,
                                    snap))
                            {
                                _usingGizmo = true;
                                arr[i] = Vector3.TransformPosition(propertyTransform.ToMatrix().ExtractTranslation(),
                                        entityTransform.Inverted());

                                SelectedEntity.SetPropertyValue(gizmoProperty.Name, arr.ToArray());
                            }
                        }
                    }
                }
            }
        }
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (_usingGizmo)
            return true;

        if (Enabled)
        {
            if (NoclipMove(keyboardState, mouseState, frameTime))
                return true;

            var screenspacePosition = new OpenTK.Mathematics.Vector2(mouseState.Position.X / Engine.MainViewport.Size.X, mouseState.Y / Engine.MainViewport.Size.Y);
            var ray = Engine.MainViewport.GetCameraToViewportRay(screenspacePosition);

            var control = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            var alt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);

            if (control && keyboardState.IsKeyPressed(Keys.S))
            {
                ConVarStorage.Execute("map_save");
                return true;
            }

            if (keyboardState.IsKeyPressed(Keys.LeftBracket))
                GridSize = Math.Max(1, GridSize / 2);
            if (keyboardState.IsKeyPressed(Keys.RightBracket))
                GridSize = Math.Min(1024, GridSize * 2);

            _tool.Input(keyboardState, mouseState, ray, control, alt);
        }

        if (keyboardState.IsKeyPressed(Keys.V))
        {
            Enabled = !Enabled;

            // unpause if going from editor to game mode
            if (Enabled && Engine.Paused)
            {
                Engine.Paused = false;
            }

            // pause if going from game to editor mode
            if (!Enabled && !Engine.Paused)
            {
                Engine.Paused = true;
            }
            return true;
        }

        return false;
    }

    public void ResetTool()
    {
        SetTool(_defaultTool);
    }

    private void SetTool(IEditorTool tool)
    {
        _tool.Selected = false;
        _tool.Reset();

        tool.Selected = true;

        _tool = tool;
    }

    // finds the surface to start drawing a block on: either a world mesh face or the ground plane
    public static bool FindBlockSurface(Ray ray, out Vector3 point, out int axis, out float direction)
    {
        if (Trace.IntersectsWorldMesh(ray, out _, out point, out var normal) != null)
        {
            var absNormal = new Vector3(MathF.Abs(normal.X), MathF.Abs(normal.Y), MathF.Abs(normal.Z));
            axis = absNormal.X >= absNormal.Y && absNormal.X >= absNormal.Z ? 0 : absNormal.Y >= absNormal.Z ? 1 : 2;
            direction = MathF.Sign(normal[axis]);

            // get rid of floating point errors so blocks line up with the surface
            point[axis] = MathF.Round(point[axis], 2);
            return true;
        }

        axis = 1;
        direction = ray.Origin.Y >= 0 ? 1 : -1;

        var t = -ray.Origin.Y / ray.Direction.Y;
        point = ray.Origin + ray.Direction * t;

        return MathF.Abs(ray.Direction.Y) > 1e-6f && t > 0;
    }

    public static Vector3 SnapToGrid(Vector3 point, int skipAxis)
    {
        if (!ConVarStorage.Get<bool>("edt_snap"))
            return point;

        var gridSize = ConVarStorage.Get<int>("edt_gridsize");
        for (var i = 0; i < 3; i++)
        {
            if (i != skipAxis)
                point[i] = MathF.Round(point[i] / gridSize) * gridSize;
        }

        return point;
    }

    private void AddProperty(BaseEntity entity, EntityProperty entityProperty)
    {
        if (entity.Loaded)
        {
            if (!entityProperty.Editable)
            {
                ImGui.Text($"{entityProperty.Name}: {entityProperty.Value}");
                return;
            }
        }

        var propertyName = entityProperty.Name;
        var elementLabel = propertyName;

        if (entityProperty.Type == typeof(OpenTK.Mathematics.Vector2))
        {
            var valueCasted = (OpenTK.Mathematics.Vector2)entityProperty.Value!;

            var val = new Vector2(valueCasted.X, valueCasted.Y);
            if (ImGui.DragFloat2(elementLabel, ref val))
            {
                entity.SetPropertyValue(propertyName, (OpenTK.Mathematics.Vector2)val);
            }
        }
        else if (entityProperty.Type == typeof(Vector3))
        {
            var valueCasted = (Vector3)entityProperty.Value!;

            var val = valueCasted.ToNumericsVector();
            if (ImGui.DragFloat3(elementLabel, ref val))
            {
                entity.SetPropertyValue(propertyName, (Vector3)val);
            }
        }
        else if (entityProperty.Type == typeof(Vector3[]))
        {
            ImGui.Text(elementLabel);
            var valueCasted = (Vector3[])((Vector3[])entityProperty.Value!).Clone();
            var updated = false;
            for (var i = 0; i < valueCasted.Length; i++)
            {
                var val = valueCasted[i].ToNumericsVector();
                if (ImGui.DragFloat3($"##{elementLabel}_{i}", ref val))
                {
                    valueCasted[i] = (Vector3)val;
                    updated = true;
                }
            }

            if (updated)
                entity.SetPropertyValue(propertyName, valueCasted);
        }
        else if (entityProperty.Type == typeof(Color4<Rgba>))
        {
            var valueCasted = (Color4<Rgba>)entityProperty.Value!;

            var val = new System.Numerics.Vector4(valueCasted.X, valueCasted.Y, valueCasted.Z, valueCasted.W);
            if (ImGui.ColorEdit4(elementLabel, ref val))
            {
                entity.SetPropertyValue(propertyName, new Color4<Rgba>(val.X, val.Y, val.Z, val.W));
            }
        }
        else if (entityProperty.Type == typeof(Color3<Rgb>))
        {
            var valueCasted = (Color3<Rgb>)entityProperty.Value!;

            var val = new System.Numerics.Vector3(valueCasted.X, valueCasted.Y, valueCasted.Z);
            if (ImGui.ColorEdit3(elementLabel, ref val))
            {
                entity.SetPropertyValue(propertyName, new Color3<Rgb>(val.X, val.Y, val.Z));
            }
        }
        else if (entityProperty.Type == typeof(Quaternion))
        {
            var valueCasted = (Quaternion)entityProperty.Value!;
            var eulerAngles = valueCasted.ToEulerAngles();

            var val = new System.Numerics.Vector3(MathHelper.RadiansToDegrees(eulerAngles.X),
                MathHelper.RadiansToDegrees(eulerAngles.Y),
                MathHelper.RadiansToDegrees(eulerAngles.Z));

            if (ImGui.DragFloat3(elementLabel, ref val, 1f, -360.0f, 360.0f))
            {
                entity.SetPropertyValue(propertyName,
                    new Quaternion(MathHelper.DegreesToRadians(val.X),
                        MathHelper.DegreesToRadians(val.Y),
                        MathHelper.DegreesToRadians(val.Z)));
            }
        }
        else if (entityProperty.Type == typeof(bool))
        {
            var val = (bool)entityProperty.Value!;
            if (ImGui.Checkbox(elementLabel, ref val))
            {
                entity.SetPropertyValue(propertyName, val);
            }
        }
        else if (entityProperty.Type == typeof(int))
        {
            var val = (int)entityProperty.Value!;
            if (ImGui.DragInt(elementLabel, ref val))
            {
                entity.SetPropertyValue(propertyName, val);
            }
        }
        else if (entityProperty.Type == typeof(float))
        {
            var val = (float)entityProperty.Value!;
            var speed = val > 10.0f ? 1.0f : val > 1.0f ? 0.1f : 0.01f;
            if (ImGui.DragFloat(elementLabel, ref val, speed))
            {
                entity.SetPropertyValue(propertyName, val);
            }
        }
        else if (entityProperty.Type == typeof(string))
        {
            var val = (string?)entityProperty.Value ?? string.Empty;
            if (entityProperty.PossibleValues != null)
            {
                var possibleValues = entityProperty.PossibleValues.Cast<string>().ToArray();
                if (possibleValues.Length > 0)
                {
                    var currentItem = Array.IndexOf(possibleValues, possibleValues.FirstOrDefault(x => x == val));
                    if (ImGui.Combo(elementLabel, ref currentItem, possibleValues, possibleValues.Length))
                    {
                        entity.SetPropertyValue(propertyName, possibleValues[currentItem]);
                    }
                }
            }
            else
            {
                if (ImGui.InputText(elementLabel, ref val, 1024))
                {
                    entity.SetPropertyValue(propertyName, val);
                }
                if (entityProperty.Flags == EntityPropertyFlags.FilePath && ImGui.Button("Open"))
                {
                    OpenFileDialog openFileDialog = new(Path.GetDirectoryName(val) ?? "");
                    openFileDialog.Show((_, result) =>
                    {
                        if (result == DialogResult.Ok && openFileDialog.SelectedFile != null)
                        {
                            var relativePath = Path.GetRelativePath(Environment.CurrentDirectory, openFileDialog.SelectedFile);
                            entity.SetPropertyValue(propertyName, relativePath);
                        }
                    });
                }
            }
        }
        else if (entityProperty.Type.IsEnum)
        {
            if (ImGui.BeginListBox(propertyName))
            {
                var val = (Enum?)entityProperty.Value;
                foreach (var enumOption in Enum.GetNames(entityProperty.Type))
                {
                    var parsedValue = Enum.Parse(entityProperty.Type, enumOption);
                    if (ImGui.MenuItem(enumOption, "", Equals(val, parsedValue)))
                    {
                        // dynamic so T resolves to the concrete enum type instead of Enum
                        entity.SetPropertyValue(propertyName, (dynamic)parsedValue);
                    }
                }

                ImGui.EndListBox();
            }
        }
        else
        {
            ImGui.Text($"{entityProperty.Name}: {entityProperty.Value}");
        }
    }

    private bool NoclipMove(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        var cameraSpeed = keyboardState.IsKeyDown(Keys.LeftShift) ? camera_speed * 4 : camera_speed;

        if (mouseState.IsButtonDown(MouseButton.Right))
        {
            var position = Engine.MainViewport.Position;

            if (keyboardState.IsKeyDown(Keys.W))
                position += Engine.MainViewport.Front * cameraSpeed * frameTime; // Forward
            if (keyboardState.IsKeyDown(Keys.S))
                position -= Engine.MainViewport.Front * cameraSpeed * frameTime; // Backwards
            if (keyboardState.IsKeyDown(Keys.A))
                position -= Engine.MainViewport.Right * cameraSpeed * frameTime; // Left
            if (keyboardState.IsKeyDown(Keys.D))
                position += Engine.MainViewport.Right * cameraSpeed * frameTime; // Right
            if (keyboardState.IsKeyDown(Keys.Space))
                position += Engine.MainViewport.Up * cameraSpeed * frameTime; // Up
            if (keyboardState.IsKeyDown(Keys.LeftControl))
                position -= Engine.MainViewport.Up * cameraSpeed * frameTime; // Down

            Engine.MainViewport.Position = position;
            Engine.MainViewport.Yaw += mouseState.Delta.X * sensitivity;
            Engine.MainViewport.Pitch -= mouseState.Delta.Y * sensitivity;

            if (!Engine.InputManager.IsControllingCursor)
            {
                Engine.InputManager.CaptureInput(this);
                Engine.InputManager.IsControllingCursor = true;
            }

            return true;
        }

        if (Engine.InputManager.IsControllingCursor)
        {
            Engine.InputManager.ReleaseInput(this);
            Engine.InputManager.IsControllingCursor = false;
        }

        return false;
    }
}
