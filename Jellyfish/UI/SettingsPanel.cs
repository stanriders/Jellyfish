using System;
using System.Linq;
using ImGuiNET;

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

            if (ShowPanel && ImGui.Begin("Settings", ImGuiWindowFlags.AlwaysAutoResize))
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

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();

                    if (ImGui.Button("Save"))
                    {
                        Settings.Save();
                    }
                }
                ImGui.End();
            }
        }
    }
}
