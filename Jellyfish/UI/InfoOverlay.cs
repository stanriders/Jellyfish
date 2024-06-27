using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Jellyfish.Entities;

namespace Jellyfish.UI;
public class InfoOverlay : IUiPanel
{
    private const float pad = 10.0f;
    private const int frametime_buffer_size = 10;

    private readonly List<double> _lastFiveFrametimes = new();
    private double _lastAverageFrametime;

    public void Frame()
    {
        // smoothing out frametime a bit
        if (_lastFiveFrametimes.Count > frametime_buffer_size)
        {
            _lastAverageFrametime = _lastFiveFrametimes.Average();
            _lastFiveFrametimes.Clear();
        }
        _lastFiveFrametimes.Add(MainWindow.Frametime);

        var windowFlags = ImGuiWindowFlags.NoDecoration |
                          ImGuiWindowFlags.AlwaysAutoResize |
                          ImGuiWindowFlags.NoSavedSettings |
                          ImGuiWindowFlags.NoFocusOnAppearing |
                          ImGuiWindowFlags.NoNav |
                          ImGuiWindowFlags.NoMove;

        var viewport = ImGui.GetMainViewport();
        var workPos = viewport.WorkPos; // Use work area to avoid menu-bar/task-bar, if any!
        var windowPos = new Vector2(workPos.X + pad, workPos.Y + pad);
        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.2f); // Transparent background

        if (ImGui.Begin("InfoOverlay", windowFlags))
        {
            ImGui.Text("Jellyfish");

            if (Camera.Instance != null)
            {
                ImGui.Separator();
                ImGui.Text($"FPS: {1.0 / _lastAverageFrametime:N0} (frametime: {_lastAverageFrametime * 1000.0:N4})");
                ImGui.Separator();
                ImGui.Text($"Position: {Camera.Instance.GetPropertyValue<OpenTK.Mathematics.Vector3>("Position"):N4}");
                ImGui.Separator();
                ImGui.Text($"Rotation: {Camera.Instance.GetPropertyValue<OpenTK.Mathematics.Quaternion>("Rotation").ToEulerAngles().ToDegrees():N2}");
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
        }

        ImGui.End();
    }
}