using System;
using System.Collections.Generic;
using Hexa.NET.ImGui;
using Jellyfish.Console;
using Jellyfish.Input;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vector2 = System.Numerics.Vector2;

namespace Jellyfish.UI
{
    public class ConsolePanel : IUiPanel, IInputHandler
    {
        private bool _screllToBottom;
        private bool _isEnabled;

        private readonly List<string> _history = new();
        private int _historyPosition = 0;
        private string _currentCommandInput = string.Empty;

        public ConsolePanel()
        {
            Engine.InputManager.RegisterInputHandler(this);
        }

        public void Frame(double timeElapsed)
        {
            if (!_isEnabled)
                return;

            if (ImGui.Begin("Console"))
            {
                if (ImGui.Button("All ConVars"))
                {
                    ConsoleSink.Buffer.Add(new ConsoleLine
                    {
                        Color = Color4.Gray, 
                        Timestamp = DateTime.Now, 
                        Text = string.Join('\n', ConVarStorage.ConVarNames),
                        Context = "Console"
                    });
                }
                var viewport = ImGui.GetMainViewport();
                ImGui.SetWindowSize(new Vector2(viewport.Size.X * 0.75f, viewport.Size.Y * 0.75f), ImGuiCond.FirstUseEver);

                var currentSize = ImGui.GetWindowSize();
                if (ImGui.BeginChild("ScrollingRegion", new Vector2(0, currentSize.Y - 90), ImGuiChildFlags.NavFlattened, ImGuiWindowFlags.NoMove))
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
                            var contextColor = new System.Numerics.Vector4(consoleLine.Color.X, consoleLine.Color.Y, consoleLine.Color.Z, 0.75f);
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

                ImGui.PushItemWidth(currentSize.X - 30);
                if (ImGui.InputText("##Command", ref _currentCommandInput, 255,
                        ImGuiInputTextFlags.CtrlEnterForNewLine | ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    _history.Add(_currentCommandInput);
                    ConsoleSink.Buffer.Add(new ConsoleLine
                    {
                        Color = Color4.Gray,
                        Timestamp = DateTime.Now,
                        Text = _currentCommandInput,
                        Context = "Console"
                    });

                    var commandSplit = _currentCommandInput.Split(' ');
                    var commandConvar = commandSplit[0];

                    var convar = ConVarStorage.GetConVar(commandConvar);
                    if (convar == null)
                    {
                        Log.Context(this).Error("ConVar {S} is not found!", commandConvar);
                    }
                    else
                    {
                        var commandValue = JsonConvert.DeserializeObject(commandSplit[1], convar.Type);
                        if (commandValue != null)
                        {
                            ConVarStorage.Set(commandConvar, commandValue);
                        }
                    }

                    _currentCommandInput = string.Empty;
                }
                ImGui.PopItemWidth();
            }
            ImGui.End();
        }

        public void Unload()
        {
            _history.Clear();
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
        {
            if (keyboardState.IsKeyPressed(Keys.GraveAccent))
            {
                _isEnabled = !_isEnabled;
                if (_isEnabled && !Engine.Paused)
                    Engine.Paused = true;

                return true;
            }

            if (keyboardState.IsKeyPressed(Keys.Up))
            {
                if (_history.Count == 1 || _historyPosition == 0)
                    _currentCommandInput = _history[0];
                else
                    _currentCommandInput = _history[^_historyPosition];

                if (_historyPosition < _history.Count)
                    _historyPosition++;

                return true;
            }

            if (keyboardState.IsKeyPressed(Keys.Down))
            {
                if (_history.Count == 1)
                    _currentCommandInput = string.Empty;
                else if (_historyPosition == 0)
                    _currentCommandInput = _history[0];
                else
                            _currentCommandInput = _history[^_historyPosition];
                if (_historyPosition > 0)
                    _historyPosition--;

                return true;
            }

            return false;
        }
    }
}
