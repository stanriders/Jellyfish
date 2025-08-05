using Hexa.NET.ImGui;
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

        public void Frame(double timeElapsed)
        {
            if (!_isEnabled)
                return;

            if (ImGui.Begin("Console"))
            {
                var currentSize = ImGui.GetWindowSize();
                if (currentSize is { X: 32, Y: 41 }) // default size
                {
                    var viewport = ImGui.GetMainViewport();
                    ImGui.SetWindowSize(new Vector2(viewport.Size.X * 0.75f, viewport.Size.Y * 0.75f));
                }

                if (ImGui.BeginChild("ScrollingRegion", new Vector2(0, 0), ImGuiChildFlags.NavFlattened, ImGuiWindowFlags.NoMove))
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
                            var contextColor = new System.Numerics.Vector4(consoleLine.Color.X, consoleLine.Color.Y, consoleLine.Color.Z, 0.5f);
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
                }
                ImGui.EndChild();
            }
            ImGui.End();
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
        {
            if (keyboardState.IsKeyPressed(Keys.GraveAccent))
            {
                _isEnabled = !_isEnabled;
                if (_isEnabled && !MainWindow.Paused)
                    MainWindow.Paused = true;
                return true;
            }

            return false;
        }
    }
}
