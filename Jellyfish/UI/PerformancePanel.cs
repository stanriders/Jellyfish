using ImGuiNET;
using Jellyfish.Debug;
using Jellyfish.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.UI
{
    public class PerformancePanel : IUiPanel, IInputHandler
    {
        private bool _isEnabled;
        private Dictionary<string, double> _measurements = new();
        private double _elapsedSinceLastUpdate;

        public PerformancePanel()
        {
            InputManager.RegisterInputHandler(this);
        }

        public void Frame(double timeElapsed)
        {
            if (!_isEnabled)
                return;

            if (_elapsedSinceLastUpdate > 0.1)
            {
                _measurements = PerformanceMeasurment.Measurements.ToDictionary();
                _elapsedSinceLastUpdate = 0;
            }

            if (ImGui.Begin("Performance"))
            {
                foreach (var measurement in _measurements)
                {
                    ImGui.Text(
                        $"{measurement.Key}: {1000.0 / measurement.Value:N1} ({measurement.Value:N4})");
                }
            }

            _elapsedSinceLastUpdate += timeElapsed;
        }

        public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
        {
            if (keyboardState.IsKeyPressed(Keys.M))
            {
                _isEnabled = !_isEnabled;
                return true;
            }

            return false;
        }
    }
}
