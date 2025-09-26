using System;
using System.Linq;
using Hexa.NET.ImGui;
using Jellyfish.Console;

namespace Jellyfish.UI
{
    public class SettingsPanel : IUiPanel
    {
        public static bool ShowPanel { get; set; }

        private readonly Settings.Integer2Serializable[] _resolutions = new[]
        {
            new Settings.Integer2Serializable(2560, 1440),
            new Settings.Integer2Serializable(1920, 1080), 
            new Settings.Integer2Serializable(1280, 720), 
            new Settings.Integer2Serializable(640, 480)
        };

        public void Frame(double timeElapsed)
        {
            var config = Settings.Instance;

            if (!ShowPanel)
                return;

            if (ImGui.Begin("Settings", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ShowPanel = !ImGui.Button("X");

                if (ImGui.BeginTabBar("tab"))
                {
                    if (ImGui.BeginTabItem("Audio"))
                    {
                        var volume = config.Audio.Volume;
                        ImGui.DragFloat("Volume", ref volume, 0.01f, 0.0f, 1.0f);
                        config.Audio.Volume = volume;

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Video"))
                    {
                        var fullscreen = config.Video.Fullscreen;
                        ImGui.Checkbox("Fullscreen", ref fullscreen);
                        config.Video.Fullscreen = fullscreen;

                        var resolution = config.Video.WindowSize;

                        var currentResolution = Array.IndexOf(_resolutions, resolution);

                        ImGui.Combo("Resolution", ref currentResolution,
                            _resolutions.Select(x => x.ToString()).ToArray(), _resolutions.Length);

                        config.Video.WindowSize = _resolutions[currentResolution];

                        ImGui.Separator();

                        var gtaoEnabled = ConVarStorage.Get<bool>("mat_gtao_enabled");
                        if (ImGui.Checkbox("GTAO", ref gtaoEnabled))
                            ConVarStorage.Set("mat_gtao_enabled", gtaoEnabled);

                        var gtaoQuality = ConVarStorage.Get<int>("mat_gtao_quality");
                        if (ImGui.DragInt("GTAO Quality", ref gtaoQuality, 1, 0, 3))
                            ConVarStorage.Set("mat_gtao_quality", gtaoQuality);

                        var gtaoRadius = ConVarStorage.Get<float>("mat_gtao_radius");
                        if (ImGui.DragFloat("GTAO Radius", ref gtaoRadius))
                            ConVarStorage.Set("mat_gtao_radius", gtaoRadius);

                        var gtaoIntensity = ConVarStorage.Get<float>("mat_gtao_intensity");
                        if (ImGui.DragFloat("GTAO Intensity", ref gtaoIntensity))
                            ConVarStorage.Set("mat_gtao_radius", gtaoIntensity);

                        var gtaoThickness = ConVarStorage.Get<float>("mat_gtao_thickness");
                        if (ImGui.DragFloat("GTAO Thickness", ref gtaoThickness))
                            ConVarStorage.Set("mat_gtao_radius", gtaoThickness);

                        ImGui.Separator();

                        var sslrEnabled = ConVarStorage.Get<bool>("mat_sslr_enabled");
                        if (ImGui.Checkbox("SSLR", ref sslrEnabled))
                            ConVarStorage.Set("mat_sslr_enabled", sslrEnabled);

                        ImGui.Separator();

                        var iblEnabled = ConVarStorage.Get<bool>("mat_ibl_enabled");
                        if (ImGui.Checkbox("IBL", ref iblEnabled))
                            ConVarStorage.Set("mat_ibl_enabled", iblEnabled);

                        var iblRenderWorld = ConVarStorage.Get<bool>("mat_ibl_render_world");
                        if (ImGui.Checkbox("IBL Render World", ref iblRenderWorld))
                            ConVarStorage.Set("mat_ibl_render_world", iblRenderWorld);

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();

                    if (ImGui.Button("Save"))
                    {
                        Settings.Save();
                    }
                }
            }
            ImGui.End();
        }

        public void Unload()
        {
        }
    }
}
