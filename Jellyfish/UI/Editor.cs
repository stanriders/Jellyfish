using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Widgets.Dialogs;
using Hexa.NET.ImGuizmo;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Input;
using Jellyfish.Render;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Jellyfish.UI.Components;
using Jellyfish.Utils;
using Quaternion = OpenTK.Mathematics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace Jellyfish.UI;

public class EnableEditor() : ConVar<bool>("edt_enable",
#if DEBUG
    true);
#else
    false);
#endif

public class EditorGridSize() : ConVar<int>("edt_gridsize", 16);
public class EditorSnapToGrid() : ConVar<bool>("edt_snap", true);

public class Editor : IUiPanel, IInputHandler
{
    private enum Tool
    {
        Select,
        Block,
        Faces
    }

    private enum BlockCreationStage
    {
        None,
        BaseSize,
        Height
    }

    private const float pad = 10.0f;
    private BaseEntity? _selectedEntity;
    private string? _selectedEntityType;

    private bool _usingGizmo = false;
    private static bool setUpDocking = true;

    private const float camera_speed = 120.0f;
    private const float sensitivity = 0.2f;

    // ImGui colors are ABGR
    private const uint grid_color = 0x30FFFFFF;
    private const uint grid_major_color = 0x80FFFFFF;
    private const uint face_hover_color = 0xFF00DCFF;
    private const uint face_selected_color = 0xFF0050FF;

    private Tool _tool = Tool.Select;

    // only set when the cursor is over the viewport and not over the UI
    private Ray? _mouseRay;

    private readonly List<int> _selectedFaces = new();

    // 0 - waiting for click, 1 - dragging the base rectangle, 2 - picking the height
    private BlockCreationStage _blockStage;
    private int _blockAxis;
    private float _blockDirection;
    private Vector3 _blockStart;
    private Vector3 _blockEnd;
    private float _blockHeight;
    private float _blockBevel;
    private int _blockBevelSegments = 4;

    public Editor()
    {
        Engine.InputManager.RegisterInputHandler(this);
    }

    public unsafe void Frame(double timeElapsed)
    {
        if (!ConVarStorage.Get<bool>("edt_enable"))
            return;

        if (!Engine.Loaded)
            return;

        if (_selectedEntity?.MarkedForDeath ?? false)
        {
            _selectedEntity = null;
            _selectedFaces.Clear();
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
                    if (ImGui.MenuItem("Save", "Ctrl+S"))
                    {
                        MapLoader.Save($"{Engine.CurrentMap}");
                    }
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
                    ConVarComponents.MenuItem("edt_materialbrowser", "Material browser");
                    ConVarComponents.MenuItem("edt_perfpanel", "Performance stats");
                    ImGui.EndMenu();
                }

                if (ImGui.Button("Update light probes"))
                {
                    Engine.Renderer.UpdateIBL();
                }

                ImGui.Separator();

                if (ImGui.RadioButton("Select (1)", _tool == Tool.Select))
                    SetTool(Tool.Select);
                if (ImGui.RadioButton("Block (2)", _tool == Tool.Block))
                    SetTool(Tool.Block);
                if (ImGui.RadioButton("Faces (3)", _tool == Tool.Faces))
                    SetTool(Tool.Faces);

                if (_tool == Tool.Block)
                {
                    ImGui.Separator();

                    ImGui.SetNextItemWidth(60);
                    ImGui.DragFloat("Bevel", ref _blockBevel, 0.25f, 0.0f, 1024.0f);
                    ImGui.SetNextItemWidth(60);
                    ImGui.DragInt("Segments", ref _blockBevelSegments, 0.1f, 1, 16);
                }

                ImGui.Separator();

                ConVarComponents.Checkbox("edt_snap", "Snap");
                ImGui.Text($"Grid: {ConVarStorage.Get<int>("edt_gridsize")} ([ / ])");

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
            if (ImGui.Begin("Editor params"))
            {
                ConVarComponents.Checkbox("edt_drawcones", "Enable debug cones");
                ConVarComponents.Checkbox("edt_drawnames", "Show entity names");
                ConVarComponents.Checkbox("phys_debug", "Enable physics debug overlay");
                ConVarComponents.Checkbox("audio_debug", "Enable audio debug overlay");
            }

            ImGui.End();

            ImGui.SetNextWindowBgAlpha(0.5f);
            if (ImGui.Begin("Entity controls"))
            {
                if (ImGui.BeginListBox("##Entity list", new Vector2(-1, 10 * ImGui.GetTextLineHeightWithSpacing())))
                {
                    foreach (var entity in Engine.EntityManager.Entities.OrderBy(x => x.Name).Where(x => x is not WorldMesh))
                    {
                        if (ImGui.MenuItem($"{entity.Name} ({entity.ClassName})", "", _selectedEntity?.Name == entity.Name))
                        {
                            _selectedEntity = entity;
                            _selectedFaces.Clear();
                        }
                    }

                    ImGui.EndListBox();
                }

                ImGui.Separator();

                if (_selectedEntity != null)
                {
                    ImGui.Text(_selectedEntity.GetType().ToString());
                    foreach (var entityProperty in _selectedEntity.EntityProperties)
                    {
                        AddProperty(_selectedEntity, entityProperty);
                    }

                    ImGui.Spacing();

                    foreach (var entityAction in _selectedEntity.EntityActions.OrderBy(x => x.Name))
                    {
                        if (ImGui.Button(entityAction.Name))
                            entityAction.Act();
                    }

                    ImGui.Spacing();

                    if (!_selectedEntity.Loaded)
                    {
                        if (ImGui.Button("Load"))
                        {
                            _selectedEntity.Load();
                        }
                    }

                    if (_tool == Tool.Faces && _selectedEntity is WorldMesh worldMesh)
                    {
                        DrawFaceControls(worldMesh);
                    }
                }
            }

            ImGui.End();

            ImGui.SetNextWindowBgAlpha(0.5f);
            if (ImGui.Begin("Add entity"))
            {
                if (ImGui.BeginListBox("##Entity types", new Vector2(-1, 10 * ImGui.GetTextLineHeightWithSpacing())))
                {
                    foreach (var entityClass in Engine.EntityManager.EntityClasses)
                    {
                        if (ImGui.MenuItem(entityClass, "", entityClass == _selectedEntityType))
                        {
                            _selectedEntityType = entityClass;
                        }
                    }

                    ImGui.EndListBox();
                }

                if (_selectedEntityType != null)
                {
                    if (ImGui.Button("Spawn"))
                    {
                        _selectedEntity = Engine.EntityManager.CreateEntity(_selectedEntityType);
                        _selectedFaces.Clear();
                    }
                }
            }

            ImGui.End();

            if (setUpDocking)
            {
                setUpDocking = false;

                var dockIdRight = ImGuiP.DockBuilderSplitNode(editorDock, ImGuiDir.Right, 0.15f, null, &editorDock);
                var dockIdRightTop = ImGuiP.DockBuilderSplitNode(dockIdRight, ImGuiDir.Up, 0.15f, null, &dockIdRight);
                var dockIdRightMiddle = ImGuiP.DockBuilderSplitNode(dockIdRight, ImGuiDir.Up, 0.55f, null, &dockIdRight);
                var dockIdRightBottom = ImGuiP.DockBuilderSplitNode(dockIdRight, ImGuiDir.Up, 0.3f, null, &dockIdRight);
                ImGuiP.DockBuilderDockWindow("Editor params", dockIdRightTop);
                ImGuiP.DockBuilderDockWindow("Entity controls", dockIdRightMiddle);
                ImGuiP.DockBuilderDockWindow("Add entity", dockIdRightBottom);
                ImGuiP.DockBuilderDockWindow("Materials", dockIdRightBottom);
                ImGuiP.DockBuilderFinish(dockIdRight);
                ImGuiP.DockBuilderFinish(editorDock);
            }
        }

        ImGui.End();
        ImGui.PopStyleColor();

        DrawSelectedEntityControls();

        if (_tool == Tool.Block)
            DrawBlockTool();

        if (_tool == Tool.Faces)
        {
            DrawFaceSelection();
            DrawPatchControls();
        }

        _mouseRay = null;
    }

    public void Unload()
    {
        _selectedEntity = null;
        _selectedFaces.Clear();
    }

    private unsafe void DrawSelectedEntityControls()
    {
        if (_selectedEntity == null)
            return;

        if (_selectedEntity.BoundingBox != null)
            DebugRender.DrawBoundingBox(_selectedEntity.GetPropertyValue<Vector3>("Position"),
                _selectedEntity.BoundingBox.Value);

        if (_selectedEntity is IHaveFrustum frustumEntity)
        {
            DebugRender.DrawFrustum(frustumEntity.GetFrustum());
        }

        DebugRender.DrawText(_selectedEntity.GetPropertyValue<Vector3>("Position") + new Vector3(0, 3, 0), _selectedEntity.Name ?? "null");

        _usingGizmo = false;

        // gizmos would get in the way of the other tools
        if (_tool != Tool.Select)
            return;

        fixed (float* view = Engine.MainViewport.GetViewMatrix().ToFloatArray())
        fixed (float* proj = Engine.MainViewport.GetProjectionMatrix().ToFloatArray())
        fixed (float* snap = new[] { 0.1f, 0.1f, 0.1f })
        {
            ImGuizmo.SetID(_selectedEntity.GetHashCode());

            var hasScale = _selectedEntity.CanEditProperty("Scale");
            var hasSize = _selectedEntity.CanEditProperty("Size");
            var hasRotation = _selectedEntity.CanEditProperty("Rotation");

            var sizeType = _selectedEntity.EntityProperties.SingleOrDefault(x => x.Name == "Size")?.Type;

            var sizeValue = Vector3.One;
            if (sizeType == typeof(Vector3))
                sizeValue = _selectedEntity.GetPropertyValue<Vector3>("Size");
            else if (sizeType == typeof(OpenTK.Mathematics.Vector2))
                sizeValue = new Vector3(_selectedEntity.GetPropertyValue<OpenTK.Mathematics.Vector2>("Size"), 1f);

            var entityRotation = hasRotation
                ? Matrix4.CreateFromQuaternion(_selectedEntity.GetPropertyValue<Quaternion>("Rotation"))
                : Matrix4.Identity;

            var entityScale = hasSize
                ? Matrix4.CreateScale(sizeValue)
                : hasScale
                    ? Matrix4.CreateScale(_selectedEntity.GetPropertyValue<Vector3>("Scale"))
                    : Matrix4.Identity;

            var entityTransform = entityScale * entityRotation *
                                  Matrix4.CreateTranslation(
                                      _selectedEntity.GetPropertyValue<Vector3>("Position"));
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
                    _selectedEntity.SetPropertyValue("Position",
                        transformArray.ToMatrix().ExtractTranslation());

                    if (hasRotation)
                    {
                        _selectedEntity.SetPropertyValue("Rotation",
                            transformArray.ToMatrix().ExtractRotation());
                    }

                    if (hasSize)
                    {
                        var newSize = transformArray.ToMatrix().ExtractScale();
                        if (sizeType == typeof(Vector3))
                            _selectedEntity.SetPropertyValue("Size", newSize);
                        else if (sizeType == typeof(OpenTK.Mathematics.Vector2))
                            _selectedEntity.SetPropertyValue("Size", new OpenTK.Mathematics.Vector2(newSize.X, newSize.Y));
                    }
                    else if (hasScale)
                    {
                        _selectedEntity.SetPropertyValue("Scale", transformArray.ToMatrix().ExtractScale());
                    }

                    if (_selectedEntity is IPhysicsEntity physicsEntity)
                    {
                        physicsEntity.ResetVelocity();
                    }
                }
                else
                {
                    if (_usingGizmo && _selectedEntity is IPhysicsEntity physicsEntity)
                    {
                        physicsEntity.ResetVelocity();
                    }
                }
            }

            foreach (var gizmoProperty in _selectedEntity.EntityProperties.Where(x => x.ShowGizmo))
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
                            ImGuizmo.SetID(_selectedEntity.GetHashCode() + gizmoProperty.GetHashCode() + i);

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

                                _selectedEntity.SetPropertyValue(gizmoProperty.Name, arr.ToArray());
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

        var enabled = ConVarStorage.Get<bool>("edt_enable");

        if (enabled)
        {
            // right click cancels the block that's being drawn
            if (_blockStage != 0 && mouseState.IsButtonPressed(MouseButton.Right))
                _blockStage = 0;

            if (NoclipMove(keyboardState, mouseState, frameTime))
                return true;

            var screenspacePosition = new OpenTK.Mathematics.Vector2(mouseState.Position.X / Engine.MainViewport.Size.X, mouseState.Y / Engine.MainViewport.Size.Y);
            var ray = Engine.MainViewport.GetCameraToViewportRay(screenspacePosition);
            _mouseRay = ray;

            var control = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
            var alt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);

            if (control && keyboardState.IsKeyPressed(Keys.S))
            {
                MapLoader.Save($"{Engine.CurrentMap}");
                return true;
            }

            if (keyboardState.IsKeyPressed(Keys.D1))
                SetTool(Tool.Select);
            if (keyboardState.IsKeyPressed(Keys.D2))
                SetTool(Tool.Block);
            if (keyboardState.IsKeyPressed(Keys.D3))
                SetTool(Tool.Faces);

            var gridSize = ConVarStorage.Get<int>("edt_gridsize");
            if (keyboardState.IsKeyPressed(Keys.LeftBracket))
                ConVarStorage.Set("edt_gridsize", Math.Max(1, gridSize / 2));
            if (keyboardState.IsKeyPressed(Keys.RightBracket))
                ConVarStorage.Set("edt_gridsize", Math.Min(1024, gridSize * 2));

            if (_tool == Tool.Select)
            {
                if (mouseState.IsButtonPressed(MouseButton.Left))
                {
                    var entity = Trace.IntersectsEntity(ray);
                    if (entity == _selectedEntity)
                        _selectedEntity = null;
                    else
                        _selectedEntity = entity;
                }

                if (_selectedEntity != null)
                {
                    if (keyboardState.IsKeyPressed(Keys.Delete))
                    {
                        Engine.EntityManager.KillEntity(_selectedEntity);
                        _selectedEntity = null;
                    }
                }
            }
            else if (_tool == Tool.Block)
            {
                BlockToolInput(mouseState, ray);
            }
            else if (_tool == Tool.Faces)
            {
                FaceToolInput(keyboardState, mouseState, ray, control, alt);
            }
        }

        if (keyboardState.IsKeyPressed(Keys.V))
        {
            ConVarStorage.Set("edt_enable", !enabled);

            // unpause if going from editor to game mode
            if (enabled && Engine.Paused)
            {
                Engine.Paused = false;
            }

            // pause if going from game to editor mode
            if (!enabled && !Engine.Paused)
            {
                Engine.Paused = true;
            }
            return true;
        }

        return false;
    }

    private void SetTool(Tool tool)
    {
        _tool = tool;
        _blockStage = 0;
        _selectedFaces.Clear();
    }

    private void BlockToolInput(MouseState mouseState, Ray ray)
    {
        var gridSize = ConVarStorage.Get<int>("edt_gridsize");

        if (_blockStage == BlockCreationStage.None)
        {
            if (mouseState.IsButtonPressed(MouseButton.Left) && FindBlockSurface(ray, out var point, out _blockAxis, out _blockDirection))
            {
                _blockStart = SnapToGrid(point, _blockAxis);
                _blockEnd = _blockStart;
                _blockStage = BlockCreationStage.BaseSize;
            }
        }
        else if (_blockStage == BlockCreationStage.BaseSize)
        {
            // keep the base on the plane it was started on
            var t = (_blockStart[_blockAxis] - ray.Origin[_blockAxis]) / ray.Direction[_blockAxis];
            if (MathF.Abs(ray.Direction[_blockAxis]) > 1e-6f && t > 0)
                _blockEnd = SnapToGrid(ray.Origin + ray.Direction * t, _blockAxis);

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

                var uSamples = Box.GetBevelSamples(halfSize[uAxis], radius, segments);
                var vSamples = Box.GetBevelSamples(halfSize[vAxis], radius, segments);

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

        _selectedEntity = entity;
    }

    // finds the surface to start drawing a block on: either a world mesh face or the ground plane
    private static bool FindBlockSurface(Ray ray, out Vector3 point, out int axis, out float direction)
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

    private static Vector3 SnapToGrid(Vector3 point, int skipAxis)
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

    private void DrawBlockTool()
    {
        Vector3 center;
        int axis;

        if (_blockStage == 0)
        {
            if (_mouseRay == null || !FindBlockSurface(_mouseRay.Value, out var point, out axis, out _))
                return;

            center = SnapToGrid(point, axis);
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
            DebugRender.DrawLine(uStart, uEnd, MathF.Abs(uStart[u] % majorLine) < 0.01f ? grid_major_color : grid_color);
            DebugRender.DrawLine(vStart, vEnd, MathF.Abs(vStart[v] % majorLine) < 0.01f ? grid_major_color : grid_color);
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

    private void FaceToolInput(KeyboardState keyboardState, MouseState mouseState, Ray ray, bool control, bool alt)
    {
        if (keyboardState.IsKeyPressed(Keys.Delete) && _selectedEntity is WorldMesh worldMesh && _selectedFaces.Count > 0)
        {
            var mesh = worldMesh.GetPropertyValue<EditableMesh>("Mesh")!;
            worldMesh.SetPropertyValue("Mesh", mesh with { Faces = mesh.Faces.Where((_, i) => !_selectedFaces.Contains(i)).ToArray() });
            _selectedFaces.Clear();
        }

        if (!mouseState.IsButtonPressed(MouseButton.Left))
            return;

        var entity = Trace.IntersectsWorldMesh(ray, out var face, out _, out _);

        // eyedropper
        if (alt)
        {
            if (entity != null)
                MaterialBrowser.SelectedMaterial = entity.GetPropertyValue<EditableMesh>("Mesh")!.Faces[face].Material;

            return;
        }

        if (entity != _selectedEntity || entity == null)
        {
            _selectedEntity = entity;
            _selectedFaces.Clear();
        }

        if (entity == null)
            return;

        if (!control)
        {
            _selectedFaces.Clear();
            _selectedFaces.Add(face);
        }
        else if (!_selectedFaces.Remove(face))
        {
            _selectedFaces.Add(face);
        }
    }

    private void DrawFaceSelection()
    {
        if (_mouseRay != null && Trace.IntersectsWorldMesh(_mouseRay.Value, out var hoveredFace, out _, out _) is { } hoveredEntity)
            DrawFaceOutline(hoveredEntity, hoveredFace, face_hover_color);

        if (_selectedEntity is not WorldMesh worldMesh)
            return;

        var faceCount = worldMesh.GetPropertyValue<EditableMesh>("Mesh")!.Faces.Length;
        _selectedFaces.RemoveAll(x => x >= faceCount);

        foreach (var face in _selectedFaces)
            DrawFaceOutline(worldMesh, face, face_selected_color);
    }

    private unsafe void DrawPatchControls()
    {
        if (_selectedEntity is not WorldMesh worldMesh || _selectedFaces.Count != 1)
            return;

        var mesh = worldMesh.GetPropertyValue<EditableMesh>("Mesh")!;
        var face = mesh.Faces[_selectedFaces[0]];
        if (face.Subdivisions == 0)
            return;

        var transform = worldMesh.Transform;
        var gridSize = (float)ConVarStorage.Get<int>("edt_gridsize");
        var snapping = ConVarStorage.Get<bool>("edt_snap");

        fixed (float* view = Engine.MainViewport.GetViewMatrix().ToFloatArray())
        fixed (float* proj = Engine.MainViewport.GetProjectionMatrix().ToFloatArray())
        fixed (float* snap = new[] { gridSize, gridSize, gridSize })
        {
            for (var i = 0; i < face.Indices.Length; i++)
            {
                var index = face.Indices[i];
                var pointTransform = Matrix4.CreateTranslation(Vector3.TransformPosition(mesh.Vertices[index], transform)).ToFloatArray();

                fixed (float* pointTransformPinned = pointTransform)
                {
                    ImGuizmo.SetID(i);

                    ImGuizmo.DrawCubes(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                        ref Unsafe.AsRef<float>(pointTransformPinned), 1);

                    if (ImGuizmo.Manipulate(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                            ImGuizmoOperation.Translate, ImGuizmoMode.World,
                            ref Unsafe.AsRef<float>(pointTransformPinned),
                            null,
                            snapping ? snap : null))
                    {
                        _usingGizmo = true;

                        var vertices = mesh.Vertices.ToArray();
                        vertices[index] = Vector3.TransformPosition(pointTransform.ToMatrix().ExtractTranslation(), transform.Inverted());

                        mesh = mesh with { Vertices = vertices };
                        worldMesh.SetPropertyValue("Mesh", mesh);
                    }
                }
            }
        }
    }

    private static void DrawFaceOutline(WorldMesh entity, int faceIndex, uint color)
    {
        var mesh = entity.GetPropertyValue<EditableMesh>("Mesh")!;
        var transform = entity.Transform;
        var face = mesh.Faces[faceIndex];
        var indices = face.Indices;

        if (face.Subdivisions > 0)
        {
            // outline the curved edges of the patch
            var grid = WorldMesh.TessellatePatch(mesh, face);
            var size = face.Subdivisions;
            for (var i = 0; i < size; i++)
            {
                DebugRender.DrawLine(Vector3.TransformPosition(grid[i, 0], transform), Vector3.TransformPosition(grid[i + 1, 0], transform), color);
                DebugRender.DrawLine(Vector3.TransformPosition(grid[i, size], transform), Vector3.TransformPosition(grid[i + 1, size], transform), color);
                DebugRender.DrawLine(Vector3.TransformPosition(grid[0, i], transform), Vector3.TransformPosition(grid[0, i + 1], transform), color);
                DebugRender.DrawLine(Vector3.TransformPosition(grid[size, i], transform), Vector3.TransformPosition(grid[size, i + 1], transform), color);
            }

            return;
        }

        for (var i = 0; i < indices.Length; i++)
        {
            DebugRender.DrawLine(Vector3.TransformPosition(mesh.Vertices[indices[i]], transform),
                Vector3.TransformPosition(mesh.Vertices[indices[(i + 1) % indices.Length]], transform), color);
        }
    }

    private void DrawFaceControls(WorldMesh entity)
    {
        var mesh = entity.GetPropertyValue<EditableMesh>("Mesh")!;
        _selectedFaces.RemoveAll(x => x >= mesh.Faces.Length);

        ImGui.SeparatorText("Faces");

        if (_selectedFaces.Count == 0)
        {
            ImGui.TextWrapped("Click to select faces, Ctrl+click to select multiple, Alt+click to pick a material, Delete to remove selected faces. Select a single curved face to move its control points");
            return;
        }

        var face = mesh.Faces[_selectedFaces[0]];

        ImGui.Text($"{_selectedFaces.Count} selected");
        ImGui.TextWrapped($"Material: {face.Material}");

        var offset = new Vector2(face.TextureOffset.X, face.TextureOffset.Y);
        if (ImGui.DragFloat2("Texture offset", ref offset, 0.01f))
            ModifySelectedFaces(x => x with { TextureOffset = (OpenTK.Mathematics.Vector2)offset });

        var scale = new Vector2(face.TextureScale.X, face.TextureScale.Y);
        if (ImGui.DragFloat2("Texture scale", ref scale, 0.01f, 0.01f, 100.0f))
            ModifySelectedFaces(x => x with { TextureScale = (OpenTK.Mathematics.Vector2)scale });

        var rotation = face.TextureRotation;
        if (ImGui.DragFloat("Texture rotation", ref rotation, 1.0f))
            ModifySelectedFaces(x => x with { TextureRotation = rotation });

        if (ImGui.Button("Apply selected material"))
            ModifySelectedFaces(x => x with { Material = MaterialBrowser.SelectedMaterial });

        ImGui.SameLine();
        if (ImGui.Button("Reset mapping"))
            ModifySelectedFaces(x => x with { TextureOffset = OpenTK.Mathematics.Vector2.Zero, TextureScale = OpenTK.Mathematics.Vector2.One, TextureRotation = 0 });

        // flipping a patch swaps its u and v directions, reversing the control points would scramble the rows
        ImGui.SameLine();
        if (ImGui.Button("Flip"))
            ModifySelectedFaces(x => x with
            {
                Indices = x.Subdivisions > 0
                    ? Enumerable.Range(0, 16).Select(i => x.Indices[i % 4 * 4 + i / 4]).ToArray()
                    : x.Indices.Reverse().ToArray()
            });

        var firstPatch = _selectedFaces.Select(x => mesh.Faces[x]).FirstOrDefault(x => x.Subdivisions > 0);
        if (firstPatch != null)
        {
            var subdivisions = firstPatch.Subdivisions;
            if (ImGui.DragInt("Subdivisions", ref subdivisions, 0.1f, 1, 64))
                ModifySelectedFaces(x => x.Subdivisions > 0 ? x with { Subdivisions = subdivisions } : x);

            if (ImGui.Button("Flatten"))
                ModifySelectedFaces(x => x.Subdivisions > 0 ? x with { Indices = [x.Indices[0], x.Indices[3], x.Indices[15], x.Indices[12]], Subdivisions = 0 } : x);
        }

        if (_selectedFaces.Any(x => mesh.Faces[x].Subdivisions == 0 && mesh.Faces[x].Indices.Length == 4))
        {
            if (firstPatch != null)
                ImGui.SameLine();

            if (ImGui.Button("Make curved"))
            {
                var vertices = mesh.Vertices.ToList();
                var faces = mesh.Faces.ToArray();

                foreach (var i in _selectedFaces.Where(x => faces[x].Subdivisions == 0 && faces[x].Indices.Length == 4))
                {
                    // 4x4 control points spread over the quad, corners stay shared with the neighbouring faces
                    var corners = faces[i].Indices;
                    var controlPoints = new int[16];
                    for (var row = 0; row < 4; row++)
                    {
                        for (var column = 0; column < 4; column++)
                        {
                            var index = (row, column) switch
                            {
                                (0, 0) => corners[0],
                                (0, 3) => corners[1],
                                (3, 3) => corners[2],
                                (3, 0) => corners[3],
                                _ => -1
                            };

                            if (index == -1)
                            {
                                var top = Vector3.Lerp(vertices[corners[0]], vertices[corners[1]], column / 3.0f);
                                var bottom = Vector3.Lerp(vertices[corners[3]], vertices[corners[2]], column / 3.0f);
                                vertices.Add(Vector3.Lerp(top, bottom, row / 3.0f));
                                index = vertices.Count - 1;
                            }

                            controlPoints[row * 4 + column] = index;
                        }
                    }

                    faces[i] = faces[i] with { Indices = controlPoints, Subdivisions = 8 };
                }

                entity.SetPropertyValue("Mesh", mesh with { Vertices = vertices.ToArray(), Faces = faces });
            }
        }

        return;

        void ModifySelectedFaces(Func<EditableFace, EditableFace> modify)
        {
            var currentMesh = entity.GetPropertyValue<EditableMesh>("Mesh")!;
            var faces = currentMesh.Faces.ToArray();
            foreach (var i in _selectedFaces)
                faces[i] = modify(faces[i]);

            entity.SetPropertyValue("Mesh", currentMesh with { Faces = faces });
        }
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
        else if (entityProperty.Type.BaseType == typeof(Enum))
        {
            var val = (int)entityProperty.Value!;
            if (ImGui.DragInt(elementLabel, ref val))
            {
                entity.SetPropertyValue(propertyName, val);
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
