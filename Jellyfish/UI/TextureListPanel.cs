using System.Linq;
using System.Numerics;
using ImGuiNET;
using Jellyfish.Console;
using Jellyfish.Input;
using Jellyfish.Render;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.UI;

public class TextureListPanel : IUiPanel, IInputHandler
{
    private const int item_width = 250;
    private bool _isEnabled;
    private int? _expandedTexture;

    public TextureListPanel()
    {
        InputManager.RegisterInputHandler(this);
    }

    public void Frame(double timeElapsed)
    {
        if (!ConVarStorage.Get<bool>("edt_enable"))
            return;

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
                var expanded = _expandedTexture == i;

                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + item_width);
                if (expanded)
                    ImGui.Text($"{texture.Path}: {(texture.Srgb ? "[SRGB] " : "")}{texture.References} references, {texture.Levels} levels, {texture.Format}");
                else
                    ImGui.Text($"{texture.Path} ({texture.References} references)");
                ImGui.PopTextWrapPos();

                var size = expanded ? item_width * 2 : item_width;
                bool pressed;
                // flip RTs upside down
                if (texture.Path.StartsWith("_rt_"))
                    pressed = ImGui.ImageButton(texture.Path, texture.Handle, new Vector2(size, size), Vector2.One,
                        Vector2.Zero);
                else
                    pressed = ImGui.ImageButton(texture.Path, texture.Handle, new Vector2(size, size));

                if (pressed)
                {
                    _expandedTexture = i;
                }

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