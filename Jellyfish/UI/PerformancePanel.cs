using ImGuiNET;
using ImPlotNET;
using Jellyfish.Debug;
using Jellyfish.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Jellyfish.UI
{
    public class PerformancePanel : IUiPanel, IInputHandler
    {
        private bool _isEnabled;
        private ImmutableSortedDictionary<string, double>? _measurements;
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
                if (_measurements != null)
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
                }

                _measurements = PerformanceMeasurment.Measurements.ToImmutableSortedDictionary();
                _elapsedSinceLastUpdate = 0;
            }

            _elapsedSinceLastUpdate += timeElapsed;

            if (!_isEnabled || _measurements == null)
                return;

            if (ImGui.Begin("Performance"))
            {
                if (ImGui.BeginTable("Measurments", 2, ImGuiTableFlags.SizingFixedFit))
                {
                    foreach (var measurement in _measurements)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(measurement.Key);
                        ImGui.TableNextColumn();
                        ImGui.Text($"{measurement.Value:N4} ({1000.0 / measurement.Value:N1} fps)");
                    }
                    ImGui.EndTable();
                }

                var tableSize = ImGui.GetItemRectSize();
                var frameSize = ImGui.GetWindowSize();

                ImPlot.SetNextAxesToFit();
                if (ImPlot.BeginPlot("Measurements", new Vector2(frameSize.X - 30, frameSize.Y - tableSize.Y - 40), ImPlotFlags.NoInputs | ImPlotFlags.NoTitle))
                {
                    foreach (var previousMeasurement in _previousMeasurements)
                    {
                        var previousMeasurements = _previousMeasurements[previousMeasurement.Key].Select(x => (float)x).ToArray();

                        ImPlot.SetNextFillStyle(new Vector4(0,0,0,-1), 0.75f);
                        fixed (float* previousMeasurementsPinned = previousMeasurements)
                            ImPlot.PlotShaded(previousMeasurement.Key, ref Unsafe.AsRef<float>(previousMeasurementsPinned),
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
