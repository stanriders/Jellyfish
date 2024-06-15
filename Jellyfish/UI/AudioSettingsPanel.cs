using ImGuiNET;
using Jellyfish.Audio;

namespace Jellyfish.UI
{
    public class AudioSettingsPanel : IUiPanel
    {
        public void Frame()
        {
            if (ImGui.Begin("Audio Settings"))
            {
                var volume = AudioManager.Volume;
                ImGui.DragFloat("Volume", ref volume, 0.01f, 0.0f, 1.0f);
                AudioManager.Volume = volume;
            }
        }
    }
}
