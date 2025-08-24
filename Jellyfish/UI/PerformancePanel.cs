using System;
using Hexa.NET.ImGui;
using Hexa.NET.ImPlot;
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
        private ImmutableSortedDictionary<string, double>? _timedMeasurements;
        private ImmutableSortedDictionary<string, double>? _incrementalMeasurements;
        private double _elapsedSinceLastUpdate;

        private readonly Dictionary<string, List<double>> _previousTimedMeasurements = new();
        private readonly Dictionary<string, List<double>> _previousIncrementalMeasurements = new();

        public PerformancePanel()
        {
            Engine.InputManager.RegisterInputHandler(this);
        }

        public unsafe void Frame(double timeElapsed)
        {
            if (_elapsedSinceLastUpdate > 0.075)
            {
                if (_timedMeasurements != null)
                {
                    foreach (var measurement in _timedMeasurements)
                    {
                        if (!_previousTimedMeasurements.ContainsKey(measurement.Key))
                        {
                            _previousTimedMeasurements.Add(measurement.Key, new List<double>());
                        }

                        _previousTimedMeasurements[measurement.Key].Add(measurement.Value);

                        if (_previousTimedMeasurements[measurement.Key].Count > 100)
                            _previousTimedMeasurements[measurement.Key].RemoveAt(0);
                    }
                }

                _timedMeasurements = PerformanceMeasurment.TimedMeasurements.ToImmutableSortedDictionary();

                if (_incrementalMeasurements != null)
                {
                    foreach (var measurement in _incrementalMeasurements)
                    {
                        if (!_previousIncrementalMeasurements.ContainsKey(measurement.Key))
                        {
                            _previousIncrementalMeasurements.Add(measurement.Key, new List<double>());
                        }

                        _previousIncrementalMeasurements[measurement.Key].Add(measurement.Value);

                        if (_previousIncrementalMeasurements[measurement.Key].Count > 100)
                            _previousIncrementalMeasurements[measurement.Key].RemoveAt(0);
                    }
                }

                _incrementalMeasurements = PerformanceMeasurment.IncrementalMeasurements.ToImmutableSortedDictionary();
                _elapsedSinceLastUpdate = 0;
            }

            _elapsedSinceLastUpdate += timeElapsed;

            if (!_isEnabled || _timedMeasurements == null || _incrementalMeasurements == null)
                return;

            if (ImGui.Begin("Performance"))
            {
                if (ImGui.BeginTable("Measurements", 2, ImGuiTableFlags.SizingFixedFit))
                {
                    foreach (var measurement in _timedMeasurements)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(measurement.Key);
                        ImGui.TableNextColumn();
                        ImGui.Text($"{measurement.Value:N4} ({1000.0 / measurement.Value:N1} fps)");
                    }

                    ImGui.Separator();
                    foreach (var measurement in _incrementalMeasurements)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(measurement.Key);
                        ImGui.TableNextColumn();
                        ImGui.Text(measurement.Value.ToString());
                    }
                    ImGui.EndTable();
                }

                var tableSize = ImGui.GetItemRectSize();
                var frameSize = ImGui.GetWindowSize();

                ImPlot.SetNextAxesToFit();
                if (ImPlot.BeginPlot("TimedMeasurementsPlot", new Vector2(Math.Max(1, frameSize.X - 30), Math.Max(1, frameSize.Y - tableSize.Y - 40)), ImPlotFlags.NoInputs | ImPlotFlags.NoTitle))
                {
                    foreach (var previousMeasurement in _previousTimedMeasurements)
                    {
                        var previousMeasurements = _previousTimedMeasurements[previousMeasurement.Key].Select(x => (float)x).ToArray();

                        ImPlot.SetNextFillStyle(new Vector4(0,0,0,-1), 0.75f);
                        fixed (float* previousMeasurementsPinned = previousMeasurements)
                            ImPlot.PlotShaded(previousMeasurement.Key, ref Unsafe.AsRef<float>(previousMeasurementsPinned),
                                previousMeasurements.Length);
                    }

                    ImPlot.EndPlot();
                }

                ImPlot.SetNextAxesToFit();
                if (ImPlot.BeginPlot("IncrementalMeasurementsPlot", new Vector2(Math.Max(1, frameSize.X - 30), Math.Max(1, frameSize.Y - tableSize.Y - 40)), ImPlotFlags.NoInputs | ImPlotFlags.NoTitle))
                {
                    foreach (var previousMeasurement in _previousIncrementalMeasurements)
                    {
                        var previousMeasurements = _previousIncrementalMeasurements[previousMeasurement.Key].Select(x => (float)x).ToArray();

                        ImPlot.SetNextFillStyle(new Vector4(0, 0, 0, -1), 0.75f);
                        fixed (float* previousMeasurementsPinned = previousMeasurements)
                            ImPlot.PlotShaded(previousMeasurement.Key, ref Unsafe.AsRef<float>(previousMeasurementsPinned),
                                previousMeasurements.Length);
                    }

                    ImPlot.EndPlot();
                }
            }
            ImGui.End();
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
