using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Jellyfish.Console;
using Jellyfish.Entities;

namespace Jellyfish.UI;

public class EnableEditor() : ConVar<bool>("edt_enable", true);

public class InfoOverlay : IUiPanel
{
    private const float pad = 10.0f;
    private const int frametime_buffer_size = 30;

    private readonly List<double> _lastFewFrametimes = new();
    private double _lastAverageFrametime;

    private string _mapInput = string.Empty;

    public void Frame()
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
                          ImGuiWindowFlags.NoMove;

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
                ImGui.Text($"{MainWindow.CurrentMap}");

                if (Player.Instance != null)
                {

                    ImGui.Separator();
                    ImGui.Text(
                        $"Position: {Player.Instance.GetPropertyValue<OpenTK.Mathematics.Vector3>("Position"):N4}");
                    ImGui.Separator();
                    ImGui.Text(
                        $"Rotation: {Player.Instance.GetPropertyValue<OpenTK.Mathematics.Quaternion>("Rotation").ToEulerAngles().ToDegrees():N2}");
                }
            }

            ImGui.Separator();
            ImGui.InputText("", ref _mapInput, 1024);
            ImGui.SameLine();
            if (ImGui.Button("Load"))
            {
                MainWindow.QueuedMap = $"maps/{_mapInput}.json";
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
            ImGui.SameLine();
            ImGui.Checkbox("Editor mode", ref ConVarStorage.GetConVar<bool>("edt_enable")!.Value);
            ImGui.End();
        }

        if (ConVarStorage.Get<bool>("edt_enable"))
        {
            ImGui.SetNextWindowBgAlpha(0.2f);

            if (ImGui.Begin("GizmosOverlay", windowFlags))
            {
                var editorWindowSize = ImGui.GetWindowSize();
                var editorWindowPos = new Vector2(workPos.X + viewport.WorkSize.X - editorWindowSize.X - pad, workPos.Y + pad);
                ImGui.SetWindowPos(editorWindowPos);

                ImGui.Checkbox("Enable boxes", ref ConVarStorage.GetConVar<bool>("edt_showentityboxes")!.Value);
                ImGui.Checkbox("Enable gizmos", ref ConVarStorage.GetConVar<bool>("edt_showentitygizmos")!.Value);
                ImGui.Checkbox("Enable physics debug overlay", ref ConVarStorage.GetConVar<bool>("phys_debug")!.Value);
                ImGui.End();
            }
        }
    }
}