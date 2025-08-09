using Hexa.NET.ImGui;
using Jellyfish.Render;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Jellyfish.UI;

public class InfoOverlay : IUiPanel
{
    private const float pad = 10.0f;
    private const int frametime_buffer_size = 30;

    private readonly List<double> _lastFewFrametimes = new();
    private double _lastAverageFrametime;

    private string _mapInput = string.Empty;

    public void Frame(double timeElapsed)
    {
        // smoothing out frametime a bit
        if (_lastFewFrametimes.Count > frametime_buffer_size)
        {
            _lastAverageFrametime = _lastFewFrametimes.Average();
            _lastFewFrametimes.Clear();
        }
        _lastFewFrametimes.Add(MainWindow.Frametime);

        var windowFlags = ImGuiWindowFlags.NoDecoration |
              ImGuiWindowFlags.AlwaysAutoResize |
              ImGuiWindowFlags.NoSavedSettings |
              ImGuiWindowFlags.NoFocusOnAppearing |
              ImGuiWindowFlags.NoNav |
              ImGuiWindowFlags.NoMove | 
              ImGuiWindowFlags.NoDocking;

        var viewport = ImGui.GetMainViewport();
        var workPos = viewport.WorkPos; // Use work area to avoid menu-bar/task-bar, if any!
        var windowPos = new Vector2(workPos.X + pad, workPos.Y + pad);
        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.2f); // Transparent background

        if (ImGui.Begin("InfoOverlay", windowFlags))
        {
            ImGui.Text("Jellyfish");

            ImGui.Separator();
            ImGui.Text(
                $"FPS: {1.0 / _lastAverageFrametime:N0} (frametime: {_lastAverageFrametime * 1000.0:N4})");

            if (MainWindow.Loaded)
            {
                ImGui.Separator();
                ImGui.Text($"Position: {Camera.Instance.Position:N4}");
                ImGui.Separator();
                ImGui.Text($"Rotation: {Camera.Instance.Rotation.ToEulerAngles().ToDegrees():N2}");
            }

            ImGui.Separator();
            if (ImGui.Button("Settings"))
            {
                SettingsPanel.ShowPanel = true;
            }
            ImGui.SameLine();
            if (ImGui.Button("Quit"))
            {
                MainWindow.ShouldQuit = true;
            }
            ImGui.End();
        }
    }
}