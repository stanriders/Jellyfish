using ImGuiNET;
using ImPlotNET;
using Jellyfish.Debug;
using Jellyfish.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jellyfish.UI
{
    public class PerformancePanel : IUiPanel, IInputHandler
    {
        private bool _isEnabled;
        private Dictionary<string, double> _measurements = new();
        private double _elapsedSinceLastUpdate;

        private Dictionary<string, List<double>> _previousMeasurements = new();

        public PerformancePanel()
        {
            InputManager.RegisterInputHandler(this);
        }

        public unsafe void Frame(double timeElapsed)
        {
            if (_elapsedSinceLastUpdate > 0.1)
            {
                foreach (var measurement in _measurements)
                {
                    if (!_previousMeasurements.ContainsKey(measurement.Key))
                    {
                        _previousMeasurements.Add(measurement.Key, new List<double>());
                    }

                    _previousMeasurements[measurement.Key].Add(measurement.Value);

                    if (_previousMeasurements[measurement.Key].Count > 100)
                        _previousMeasurements[measurement.Key].RemoveAt(0);
                }

                _measurements = PerformanceMeasurment.Measurements.ToDictionary();
                _elapsedSinceLastUpdate = 0;
            }

            _elapsedSinceLastUpdate += timeElapsed;

            if (!_isEnabled)
                return;

            if (ImGui.Begin("Performance"))
            {
                foreach (var measurement in _measurements)
                {
                    ImGui.Text(
                        $"{measurement.Key}: {1000.0 / measurement.Value:N1} ({measurement.Value:N4})");

                }

                var frameSize = ImGui.GetWindowSize();

                ImPlot.SetNextAxesToFit();
                if (ImPlot.BeginPlot("Measurements", new Vector2(frameSize.X - 30, frameSize.Y - 120), ImPlotFlags.NoInputs))
                {
                    foreach (var previousMeasurement in _previousMeasurements)
                    {
                        var previousMeasurements = _previousMeasurements[previousMeasurement.Key].Select(x => (float)x).ToArray();

                        fixed (float* previousMeasurementsPinned = previousMeasurements)
                            ImPlot.PlotBars(previousMeasurement.Key, ref Unsafe.AsRef<float>(previousMeasurementsPinned),
                                previousMeasurements.Length);
                    }

                    ImPlot.EndPlot();
                }

                ImGui.End();
            }
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
