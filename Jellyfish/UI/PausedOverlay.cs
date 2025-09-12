using System.Numerics;
using Hexa.NET.ImGui;
using Jellyfish.Console;

namespace Jellyfish.UI;

public class PausedOverlay : IUiPanel
{
    public void Frame(double timeElapsed)
    {
        if (!Engine.Paused)
            return;

        var windowFlags = ImGuiWindowFlags.NoDecoration |
                          ImGuiWindowFlags.AlwaysAutoResize |
                          ImGuiWindowFlags.NoSavedSettings |
                          ImGuiWindowFlags.NoFocusOnAppearing |
                          ImGuiWindowFlags.NoNav |
                          ImGuiWindowFlags.NoMove | 
                          ImGuiWindowFlags.NoDocking;

        if (ImGui.Begin("PausedOverlay", windowFlags))
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
        }
        ImGui.End();
    }

    public void Unload()
    {
    }
}