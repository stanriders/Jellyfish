using System.Numerics;
using ImGuiNET;
using Jellyfish.Console;

namespace Jellyfish.UI;

public class PausedOverlay : IUiPanel
{
    public void Frame(double timeElapsed)
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

            // show paused text in the corner when in editor mode
            if (ConVarStorage.Get<bool>("edt_enable"))
            {
                var panelSize = ImGui.GetWindowSize();
                ImGui.SetWindowPos(new Vector2(10, viewport.Size.Y - panelSize.Y - 10));
            }
            else
                ImGui.SetWindowPos(viewport.Size / 2);

            ImGui.Text("Paused");
            ImGui.End();
        }
    }
}