﻿using ImGuiNET;
using Jellyfish.Console;
using Jellyfish.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector2 = System.Numerics.Vector2;

namespace Jellyfish.UI
{
    public class ConsolePanel : IUiPanel, IInputHandler
    {
        private bool _screllToBottom;
        private bool _isEnabled;

        public ConsolePanel()
        {
            InputManager.RegisterInputHandler(this);
        }

        public void Frame()
        {
            if (_isEnabled && ImGui.Begin("Console"))
            {
                if (ImGui.BeginChild("ScrollingRegion", new Vector2(0, 0), true, ImGuiWindowFlags.NavFlattened | ImGuiWindowFlags.NoMove))
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 2));

                    // we're not using foreach here because logs can be pushed to the buffer mid-rendering
                    // we don't really care if they don't get rendered immediately since its gonna get the full buffer on the next frame
                    //
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < ConsoleSink.Buffer.Count; i++)
                    {
                        var consoleLine = ConsoleSink.Buffer[i];
                        
                        // TODO: copy button

                        ImGui.TextColored(consoleLine.Color.ToNumericsVector(), consoleLine.Timestamp.ToLongTimeString());
                        ImGui.SameLine();

                        if (consoleLine.Context != null)
                        {
                            var contextColor = new System.Numerics.Vector4(consoleLine.Color.R, consoleLine.Color.G, consoleLine.Color.B, 0.5f);
                            ImGui.TextColored(contextColor, consoleLine.Context);
                            ImGui.SameLine();
                        }

                        if (consoleLine.Unimportant)
                            ImGui.TextColored(Color4.Gray.ToNumericsVector(), consoleLine.Text);
                        else
                            ImGui.TextWrapped(consoleLine.Text);
                    }

                    if (_screllToBottom || ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                    {
                        ImGui.SetScrollHereY(1.0f);
                    }

                    _screllToBottom = false;

                    ImGui.PopStyleVar();

                    ImGui.EndChild();
                }

                ImGui.End();
            }
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
        {
            if (keyboardState.IsKeyPressed(Keys.GraveAccent))
            {
                _isEnabled = !_isEnabled;
                return true;
            }

            return false;
        }
    }
}