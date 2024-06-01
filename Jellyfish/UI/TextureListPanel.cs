using System.Linq;
using System.Numerics;
using ImGuiNET;
using Jellyfish.Input;
using Jellyfish.Render;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.UI;

public class TextureListPanel : IUiPanel, IInputHandler
{
    private const int item_width = 250;
    private bool _isEnabled;

    public TextureListPanel()
    {
        InputManager.RegisterInputHandler(this);
    }

    public void Frame()
    {
        if (!_isEnabled)
            return;
        
        var textureCount = TextureManager.Textures.Count;

        if (ImGui.Begin("Texture list"))
        {
            ImGui.Text($"{textureCount} textures");
            for (int i = 0; i < textureCount; i++)
            {
                var texture = TextureManager.Textures.ElementAt(i);
                ImGui.BeginGroup();
                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + item_width);
                ImGui.Text(texture.Key);
                ImGui.PopTextWrapPos();

                // flip RTs upside down
                if (texture.Key.StartsWith("_rt_"))
                    ImGui.Image(texture.Value, new Vector2(item_width, item_width), Vector2.One, Vector2.Zero)
                else
                    ImGui.Image(texture.Value, new Vector2(item_width, item_width));

                ImGui.EndGroup();

                var windowSize = ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X;
                var prevGroup = ImGui.GetItemRectMax().X;
                var nextGroup = prevGroup + item_width / 2.0f; // divided by 2 to make ux slightly better
                if (i + 1 < textureCount && nextGroup < windowSize)
                    ImGui.SameLine();
            }
            ImGui.End();
        }
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (keyboardState.IsKeyPressed(Keys.T))
        {
            _isEnabled = !_isEnabled;
            return true;
        }

        return false;
    }
}