using Jellyfish.Console;
using Jellyfish.Input;
using ImGuiNET;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Jellyfish.Entities;
using ImGuizmoNET;
using OpenTK.Mathematics;
using System.Linq;
using System.Runtime.CompilerServices;
using Quaternion = OpenTK.Mathematics.Quaternion;
using Vector2 = System.Numerics.Vector2;
using Vector3 = OpenTK.Mathematics.Vector3;
using System;
using Jellyfish.Render;

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

    private const float camera_speed = 120.0f;
    private const float sensitivity = 0.2f;

    public Editor()
    {
        InputManager.RegisterInputHandler(this);
    }

    public unsafe void Frame()
    {
        if (!ConVarStorage.Get<bool>("edt_enable"))
            return;

        if (EntityManager.Entities == null || EntityManager.EntityClasses == null)
            return;

        if (!MainWindow.Loaded)
            return;

        if (_selectedEntity?.MarkedForDeath ?? false)
            _selectedEntity = null;

        var windowFlags = ImGuiWindowFlags.NoDecoration |
                          ImGuiWindowFlags.AlwaysAutoResize |
                          ImGuiWindowFlags.NoSavedSettings |
                          ImGuiWindowFlags.NoFocusOnAppearing |
                          ImGuiWindowFlags.NoNav |
                          ImGuiWindowFlags.NoMove;

        var viewport = ImGui.GetMainViewport();
        var workPos = viewport.WorkPos;
        var editorWindowSize = Vector2.Zero;

        ImGui.SetNextWindowBgAlpha(0.2f);

        if (ImGui.Begin("EditorParams", windowFlags))
        {
            editorWindowSize = ImGui.GetWindowSize();
            var editorWindowPos =
                new Vector2(workPos.X + viewport.WorkSize.X - editorWindowSize.X - pad, workPos.Y + pad);
            ImGui.SetWindowPos(editorWindowPos);

            ImGui.Checkbox("Enable debug cones", ref ConVarStorage.GetConVar<bool>("edt_drawcones")!.Value);
            ImGui.Checkbox("Show entity names", ref ConVarStorage.GetConVar<bool>("edt_drawnames")!.Value);
            ImGui.Checkbox("Enable physics debug overlay", ref ConVarStorage.GetConVar<bool>("phys_debug")!.Value);
            ImGui.End();
        }

        var entityControlsWindowFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize |
                          ImGuiWindowFlags.NoSavedSettings |
                          ImGuiWindowFlags.NoFocusOnAppearing |
                          ImGuiWindowFlags.NoMove;

        var entityControlsSize = new Vector2(310, viewport.Size.Y - workPos.Y - (pad * 3) - editorWindowSize.Y);
        var entityControlsPos =
            new Vector2(workPos.X + viewport.WorkSize.X - entityControlsSize.X - pad, 
                workPos.Y + pad + editorWindowSize.Y + pad);

        ImGui.SetNextWindowBgAlpha(0.5f);
        ImGui.SetNextWindowPos(entityControlsPos);
        ImGui.SetNextWindowSize(entityControlsSize);

        if (ImGui.Begin("Entity controls", entityControlsWindowFlags))
        {
            if (ImGui.BeginListBox("Entity list", new Vector2(300, 300)))
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
                fixed (float* view = Camera.Instance.GetViewMatrix().ToFloatArray())
                fixed (float* proj = Camera.Instance.GetProjectionMatrix().ToFloatArray())
                {
                    ImGuizmo.SetID(_selectedEntity.GetHashCode());

                    var rotation = Matrix4.CreateFromQuaternion(_selectedEntity.GetPropertyValue<Quaternion>("Rotation"));
                    var transform = (rotation * Matrix4.CreateTranslation(_selectedEntity.GetPropertyValue<Vector3>("Position")))
                        .ToFloatArray();

                    fixed (float* transformArray = transform)
                    {
                        ImGuizmo.Enable(true);

                        ImGuizmo.DrawCubes(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                            ref Unsafe.AsRef<float>(transformArray), 1);

                        if (ImGuizmo.Manipulate(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                                OPERATION.TRANSLATE | OPERATION.ROTATE, MODE.LOCAL,
                                ref Unsafe.AsRef<float>(transformArray)))
                        {
                            _usingGizmo = true;
                            _selectedEntity.SetPropertyValue("Position", transform.ToMatrix().ExtractTranslation());
                            _selectedEntity.SetPropertyValue("Rotation", transform.ToMatrix().ExtractRotation());

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

                            _usingGizmo = false;
                        }
                    }
                }

                if (_selectedEntity.BoundingBox != null)
                    Debug.DrawBoundingBox(_selectedEntity.GetPropertyValue<Vector3>("Position"), _selectedEntity.BoundingBox.Value);

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
                    if (ImGui.Button($"Load"))
                    {
                        _selectedEntity.Load();
                    }
                }
            }

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Add entity"))
            {
                if (ImGui.BeginListBox("Entity types"))
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

            ImGui.Separator();

            if (ImGui.Button("Save map"))
            {
                MapLoader.Save($"{MainWindow.CurrentMap}");
            }

            ImGui.End();
        }
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
                var screenspacePosition = new OpenTK.Mathematics.Vector2(mouseState.Position.X / MainWindow.WindowWidth, mouseState.Y / MainWindow.WindowHeight);
                var ray = Camera.Instance.GetCameraToViewportRay(screenspacePosition);

                _selectedEntity = Trace.IntersectsEntity(ray);
            }
        }

        if (keyboardState.IsKeyPressed(Keys.V))
        {
            ConVarStorage.Set("edt_enable", !enabled);

            // unpause if going from editor to game mode
            if (enabled && MainWindow.Paused)
            {
                MainWindow.Paused = false;
            }

            // pause if going from game to editor mode
            if (!enabled && !MainWindow.Paused)
            {
                MainWindow.Paused = true;
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

        if (entityProperty.Type == typeof(Vector2))
        {
            var valueCasted = (Vector2)entityProperty.Value!;

            var val = new System.Numerics.Vector2(valueCasted.X, valueCasted.Y);
            ImGui.DragFloat2(elementLabel, ref val);

            entity.SetPropertyValue(propertyName, new Vector2(val.X, val.Y));
        }
        else if (entityProperty.Type == typeof(Vector3))
        {
            var valueCasted = (Vector3)entityProperty.Value!;

            var val = valueCasted.ToNumericsVector();
            ImGui.DragFloat3(elementLabel, ref val);

            entity.SetPropertyValue(propertyName, val.ToOpentkVector());
        }
        else if (entityProperty.Type == typeof(Color4<Rgba>))
        {
            var valueCasted = (Color4<Rgba>)entityProperty.Value!;

            var val = new System.Numerics.Vector4(valueCasted.X, valueCasted.Y, valueCasted.Z, valueCasted.W);
            ImGui.DragFloat4(elementLabel, ref val, 0.01f, 0.0f, 1.0f);

            entity.SetPropertyValue(propertyName, new Color4<Rgba>(val.X, val.Y, val.Z, val.W));
        }
        else if (entityProperty.Type == typeof(Color3<Rgb>))
        {
            var valueCasted = (Color3<Rgb>)entityProperty.Value!;

            var val = new System.Numerics.Vector3(valueCasted.X, valueCasted.Y, valueCasted.Z);
            ImGui.DragFloat3(elementLabel, ref val, 0.01f, 0.0f, 1.0f);

            entity.SetPropertyValue(propertyName, new Color3<Rgb>(val.X, val.Y, val.Z));
        }
        else if (entityProperty.Type == typeof(Quaternion))
        {
            var valueCasted = (Quaternion)entityProperty.Value!;
            var eulerAngles = valueCasted.ToEulerAngles();

            var val = new System.Numerics.Vector3(MathHelper.RadiansToDegrees(eulerAngles.X),
                MathHelper.RadiansToDegrees(eulerAngles.Y),
                MathHelper.RadiansToDegrees(eulerAngles.Z));

            ImGui.DragFloat3(elementLabel, ref val, 1f, -360.0f, 360.0f);

            entity.SetPropertyValue(propertyName,
                new Quaternion(MathHelper.DegreesToRadians(val.X), MathHelper.DegreesToRadians(val.Y), MathHelper.DegreesToRadians(val.Z)));
        }
        else if (entityProperty.Type == typeof(bool))
        {
            var val = (bool)entityProperty.Value!;
            ImGui.Checkbox(elementLabel, ref val);
            entity.SetPropertyValue(propertyName, val);
        }
        else if (entityProperty.Type == typeof(int))
        {
            var val = (int)entityProperty.Value!;
            ImGui.DragInt(elementLabel, ref val);
            entity.SetPropertyValue(propertyName, val);
        }
        else if (entityProperty.Type == typeof(float))
        {
            var val = (float)entityProperty.Value!;
            var speed = val > 10.0f ? 1.0f : val > 1.0f ? 0.1f : 0.01f;
            ImGui.DragFloat(elementLabel, ref val, speed);
            entity.SetPropertyValue(propertyName, val);
        }
        else if (entityProperty.Type == typeof(string))
        {
            var val = (string?)entityProperty.Value ?? string.Empty;
            ImGui.InputText(elementLabel, ref val, 1024);
            entity.SetPropertyValue(propertyName, val);
        }
        else if (entityProperty.Type == typeof(Enum))
        {
            var val = (int)entityProperty.Value!;
            ImGui.DragInt(elementLabel, ref val);
            entity.SetPropertyValue(propertyName, val);
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
            var position = Camera.Instance.Position;

            if (keyboardState.IsKeyDown(Keys.W))
                position += Camera.Instance.Front * cameraSpeed * frameTime; // Forward 
            if (keyboardState.IsKeyDown(Keys.S))
                position -= Camera.Instance.Front * cameraSpeed * frameTime; // Backwards
            if (keyboardState.IsKeyDown(Keys.A))
                position -= Camera.Instance.Right * cameraSpeed * frameTime; // Left
            if (keyboardState.IsKeyDown(Keys.D))
                position += Camera.Instance.Right * cameraSpeed * frameTime; // Right
            if (keyboardState.IsKeyDown(Keys.Space))
                position += Camera.Instance.Up * cameraSpeed * frameTime; // Up 
            if (keyboardState.IsKeyDown(Keys.LeftControl))
                position -= Camera.Instance.Up * cameraSpeed * frameTime; // Down

            Camera.Instance.Position = position;
            Camera.Instance.Yaw += mouseState.Delta.X * sensitivity;
            Camera.Instance.Pitch -= mouseState.Delta.Y * sensitivity;

            if (!Camera.Instance.IsControllingCursor)
            {
                InputManager.CaptureInput(this);
                Camera.Instance.IsControllingCursor = true;
            }

            return true;
        }

        if (Camera.Instance.IsControllingCursor)
        {
            InputManager.ReleaseInput(this);
            Camera.Instance.IsControllingCursor = false;
        }

        return false;
    }
}