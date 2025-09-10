using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Input;
using Jellyfish.Render;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
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

public class Editor : IUiPanel, IInputHandler
{
    private const float pad = 10.0f;
    private BaseEntity? _selectedEntity;
    private string? _selectedEntityType;

    private bool _usingGizmo = false;
    private static bool setUpDocking = true;

    private const float camera_speed = 120.0f;
    private const float sensitivity = 0.2f;

    public Editor()
    {
        Engine.InputManager.RegisterInputHandler(this);
    }

    public unsafe void Frame(double timeElapsed)
    {
        if (!ConVarStorage.Get<bool>("edt_enable"))
            return;

        if (EntityManager.Entities == null || EntityManager.EntityClasses == null)
            return;

        if (!Engine.Loaded)
            return;

        if (_selectedEntity?.MarkedForDeath ?? false)
            _selectedEntity = null;

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
                    var textureBrowser = ConVarStorage.Get<bool>("edt_texturelist");
                    if (ImGui.MenuItem("Texture browser", "", ref textureBrowser))
                    {
                        ConVarStorage.Set("edt_texturelist", textureBrowser);
                    }
                    var meshBrowser = ConVarStorage.Get<bool>("edt_meshbrowser");
                    if (ImGui.MenuItem("Mesh browser", "", ref meshBrowser))
                    {
                        ConVarStorage.Set("edt_meshbrowser", meshBrowser);
                    }
                    ImGui.EndMenu();
                }
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
                var cones = ConVarStorage.Get<bool>("edt_drawcones");
                ImGui.Checkbox("Enable debug cones", ref cones);
                ConVarStorage.Set("edt_drawcones", cones);

                var drawnames = ConVarStorage.Get<bool>("edt_drawnames");
                ImGui.Checkbox("Show entity names", ref drawnames);
                ConVarStorage.Set("edt_drawnames", drawnames);

                var physdebug = ConVarStorage.Get<bool>("phys_debug");
                ImGui.Checkbox("Enable physics debug overlay", ref physdebug);
                ConVarStorage.Set("phys_debug", physdebug);
            }

            ImGui.End();

            ImGui.SetNextWindowBgAlpha(0.5f);
            if (ImGui.Begin("Entity controls"))
            {
                if (ImGui.BeginListBox("##Entity list", new Vector2(-1, 10 * ImGui.GetTextLineHeightWithSpacing())))
                {
                    foreach (var entity in EntityManager.Entities)
                    {
                        if (ImGui.MenuItem(entity.Name, "", _selectedEntity?.Name == entity.Name))
                        {
                            _selectedEntity = EntityManager.FindEntityByName(entity.Name);
                        }
                    }

                    ImGui.EndListBox();
                }

                ImGui.Separator();

                if (_selectedEntity != null)
                {
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
                }
            }

            ImGui.End();

            ImGui.SetNextWindowBgAlpha(0.5f);
            if (ImGui.Begin("Add entity"))
            {
                if (ImGui.BeginListBox("##Entity types", new Vector2(-1, 10 * ImGui.GetTextLineHeightWithSpacing())))
                {
                    foreach (var entityClass in EntityManager.EntityClasses.Order())
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
                        _selectedEntity = EntityManager.CreateEntity(_selectedEntityType);
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
                ImGuiP.DockBuilderFinish(dockIdRight);
                ImGuiP.DockBuilderFinish(editorDock);
            }
        }

        ImGui.End();
        ImGui.PopStyleColor();

        DrawGizmos();
    }

    private unsafe void DrawGizmos()
    {
        if (_selectedEntity == null) 
            return;

        _usingGizmo = false;

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
                        operations, ImGuizmoMode.World,
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
                                arr[Array.IndexOf(arr, point)] =
                                    Vector3.TransformPosition(propertyTransform.ToMatrix().ExtractTranslation(),
                                        entityTransform.Inverted());

                                _selectedEntity.SetPropertyValue(gizmoProperty.Name, arr.ToArray());
                            }
                        }
                    }
                }
            }
        }

        if (_selectedEntity.BoundingBox != null)
            DebugRender.DrawBoundingBox(_selectedEntity.GetPropertyValue<Vector3>("Position"),
                _selectedEntity.BoundingBox.Value);

        if (_selectedEntity is IHaveFrustum frustumEntity)
            DebugRender.DrawFrustum(frustumEntity.GetFrustum());
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (_usingGizmo)
            return true;

        var enabled = ConVarStorage.Get<bool>("edt_enable");

        if (enabled)
        {
            if (NoclipMove(keyboardState, mouseState, frameTime))
                return true;

            if (mouseState.IsButtonDown(MouseButton.Left))
            {
                var screenspacePosition = new OpenTK.Mathematics.Vector2(mouseState.Position.X / Engine.MainViewport.Size.X, mouseState.Y / Engine.MainViewport.Size.Y);
                var ray = Engine.MainViewport.GetCameraToViewportRay(screenspacePosition);

                _selectedEntity = Trace.IntersectsEntity(ray);
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
                entity.SetPropertyValue(propertyName, new OpenTK.Mathematics.Vector2(val.X, val.Y));
            }
        }
        else if (entityProperty.Type == typeof(Vector3))
        {
            var valueCasted = (Vector3)entityProperty.Value!;

            var val = valueCasted.ToNumericsVector();
            if (ImGui.DragFloat3(elementLabel, ref val))
            {
                entity.SetPropertyValue(propertyName, val.ToOpentkVector());
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
                    valueCasted[i] = val.ToOpentkVector();
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
                    if (ImGui.ListBox(elementLabel, ref currentItem, possibleValues, possibleValues.Length))
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