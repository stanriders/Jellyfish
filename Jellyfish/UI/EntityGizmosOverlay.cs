using System.Linq;
using System.Runtime.CompilerServices;
using ImGuiNET;
using ImGuizmoNET;
using Jellyfish.Entities;
using OpenTK.Mathematics;

namespace Jellyfish.UI;

#if DEBUG
public class EntityGizmosOverlay : IUiPanel
{
    private const float pad = 10.0f;
    private const int overlay_height = 35;
    private const int overlay_width = 150;
    private bool _enableGizmos = false;

    public unsafe void Frame()
    {
        if (EntityManager.Entities == null)
            return;

        if (!MainWindow.Loaded)
            return;

        var camera = Camera.Instance;
        if (camera == null)
            return;

        var windowFlags = ImGuiWindowFlags.NoDecoration |
                          ImGuiWindowFlags.AlwaysAutoResize |
                          ImGuiWindowFlags.NoSavedSettings |
                          ImGuiWindowFlags.NoFocusOnAppearing |
                          ImGuiWindowFlags.NoNav |
                          ImGuiWindowFlags.NoMove;

        var viewport = ImGui.GetMainViewport();
        var workPos = viewport.WorkPos;
        var windowPos = new System.Numerics.Vector2(workPos.X + viewport.WorkSize.X - overlay_width - pad, workPos.Y + pad);
        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(overlay_width, overlay_height));
        ImGui.SetNextWindowBgAlpha(0.2f);
        
        if (ImGui.Begin("GizmosOverlay", windowFlags))
        {
            ImGui.Checkbox("Enable gizmos", ref _enableGizmos);
            ImGui.End();
        }

        fixed (float* view = camera.GetViewMatrix().ToFloatArray())
        fixed (float* proj = camera.GetProjectionMatrix().ToFloatArray())
        {
            foreach (var entity in EntityManager.Entities.Where(x => x.DrawDevCone))
            {
                ImGuizmo.SetID(entity.GetHashCode());

                var rotation = Matrix4.CreateFromQuaternion(entity.GetPropertyValue<Quaternion>("Rotation"));
                var transform = (rotation * Matrix4.CreateTranslation(entity.GetPropertyValue<Vector3>("Position"))).ToFloatArray();
                
                fixed (float* transformArray = transform)
                {
                    ImGuizmo.Enable(true);

                    ImGuizmo.DrawCubes(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                        ref Unsafe.AsRef<float>(transformArray), 1);

                    if (_enableGizmos)
                    {
                        if (ImGuizmo.Manipulate(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                                OPERATION.TRANSLATE | OPERATION.ROTATE, MODE.LOCAL, ref Unsafe.AsRef<float>(transformArray)))
                        {
                            entity.SetPropertyValue("Position", transform.ToMatrix().ExtractTranslation());
                            entity.SetPropertyValue("Rotation", transform.ToMatrix().ExtractRotation());
                        }
                    }
                }
            }
        }
    }
}
#endif