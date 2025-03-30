using ImGuiNET;

namespace Jellyfish.UI;

public class PausedOverlay : IUiPanel
{
    public void Frame()
    {
        var windowFlags = ImGuiWindowFlags.NoDecoration |
                          ImGuiWindowFlags.AlwaysAutoResize |
                          ImGuiWindowFlags.NoSavedSettings |
                          ImGuiWindowFlags.NoFocusOnAppearing |
                          ImGuiWindowFlags.NoNav |
                          ImGuiWindowFlags.NoMove;

        if (MainWindow.Paused && ImGui.Begin("PausedOverlay", windowFlags))
        {
            var viewport = ImGui.GetMainViewport();
            ImGui.SetWindowPos(viewport.Size / 2);
            ImGui.Text("Paused");
            ImGui.End();
        }
    }
}