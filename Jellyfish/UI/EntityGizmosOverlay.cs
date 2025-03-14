using System.Linq;
using System.Runtime.CompilerServices;
using ImGuiNET;
using ImGuizmoNET;
using Jellyfish.Console;
using Jellyfish.Entities;
using OpenTK.Mathematics;

namespace Jellyfish.UI;

public class ShowBoxes() : ConVar<bool>("edt_showentityboxes", true);
public class ShowGizmos() : ConVar<bool>("edt_showentitygizmos", false);

public class EntityGizmosOverlay : IUiPanel
{
    private const float pad = 10.0f;
    private const int overlay_height = 60;
    private const int overlay_width = 150;

    public unsafe void Frame()
    {
        if (!ConVarStorage.Get<bool>("edt_enable"))
            return;

        if (EntityManager.Entities == null)
            return;

        if (!MainWindow.Loaded)
            return;

        var player = Player.Instance;
        if (player == null)
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
            ImGui.Checkbox("Enable boxes", ref ConVarStorage.GetConVar<bool>("edt_showentityboxes")!.Value);
            ImGui.Checkbox("Enable gizmos", ref ConVarStorage.GetConVar<bool>("edt_showentitygizmos")!.Value);
            ImGui.End();
        }

        fixed (float* view = player.GetViewMatrix().ToFloatArray())
        fixed (float* proj = player.GetProjectionMatrix().ToFloatArray())
        {
            foreach (var entity in EntityManager.Entities.Where(x => x.DrawDevCone))
            {
                ImGuizmo.SetID(entity.GetHashCode());

                var rotation = Matrix4.CreateFromQuaternion(entity.GetPropertyValue<Quaternion>("Rotation"));
                var transform = (rotation * Matrix4.CreateTranslation(entity.GetPropertyValue<Vector3>("Position"))).ToFloatArray();
                
                fixed (float* transformArray = transform)
                {
                    ImGuizmo.Enable(true);

                    if (ConVarStorage.Get<bool>("edt_showentityboxes"))
                    {
                        ImGuizmo.DrawCubes(ref Unsafe.AsRef<float>(view), ref Unsafe.AsRef<float>(proj),
                            ref Unsafe.AsRef<float>(transformArray), 1);
                    }

                    if (ConVarStorage.Get<bool>("edt_showentitygizmos"))
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