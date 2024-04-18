using ImGuiNET;
using Jellyfish.Entities;
using System.Numerics;

namespace Jellyfish.UI;
public class InfoOverlay : IUiPanel
{
    private const float pad = 10.0f;

    public void Frame()
    {
        var windowFlags = ImGuiWindowFlags.NoDecoration |
                          ImGuiWindowFlags.AlwaysAutoResize |
                          ImGuiWindowFlags.NoSavedSettings |
                          ImGuiWindowFlags.NoFocusOnAppearing |
                          ImGuiWindowFlags.NoNav;

        
        var viewport = ImGui.GetMainViewport();
        var workPos = viewport.WorkPos; // Use work area to avoid menu-bar/task-bar, if any!
        var windowPos = new Vector2(workPos.X + pad, workPos.Y + pad);
        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);

        windowFlags |= ImGuiWindowFlags.NoMove;

        ImGui.SetNextWindowBgAlpha(0.2f); // Transparent background
        if (ImGui.Begin("InfoOverlay", windowFlags))
        {
            ImGui.Text("Jellyfish");

            if (EntityManager.FindEntity("camera") is Camera camera)
            {
                ImGui.Separator();
                ImGui.Text($"Position: {camera.Position}");
            }
        }

        ImGui.End();
    }
}