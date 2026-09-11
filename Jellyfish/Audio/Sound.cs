using Jellyfish.Console;
using Jellyfish.Render;
using ManagedBass;
using OpenTK.Graphics.Vulkan;
using OpenTK.Mathematics;
using SteamAudio;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jellyfish.Audio
{
    public sealed class Sound : IDisposable
    {
        private readonly IPL.Context _iplContext;
        public bool Playing { get; private set; }
        public bool Persistent { get; set; }
        public IPL.Source Source { get; }
        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                var iplPos = value.ToIplVector();

                if (!iplPos.Equals(_iplSimulationInputs.Source.Origin))
                {
                    _iplSimulationInputs.Source.Origin = iplPos;
                    IPL.SourceSetInputs(Source, IPL.SimulationFlags.Direct, _iplSimulationInputs);
                }
            }
        }

        public bool UseAirAbsorption
        {
            get => _useAirAbsorption;
            set
            {
                _useAirAbsorption = value;
                var hasFlag = _iplSimulationInputs.DirectFlags.HasFlag(IPL.DirectSimulationFlags.AirAbsorption);
                if (hasFlag != value)
                {
                    if (!hasFlag)
                        _iplSimulationInputs.DirectFlags |= IPL.DirectSimulationFlags.AirAbsorption;
                    else
                        _iplSimulationInputs.DirectFlags &= ~IPL.DirectSimulationFlags.AirAbsorption;

                    IPL.SourceSetInputs(Source, IPL.SimulationFlags.Direct, _iplSimulationInputs);
                }
            }
        }

        public float Volume { get; set; } = 1.0f;

        private readonly MemoryStream? _audioStream;
        private readonly int? _stream;

        private Vector3 _position = Vector3.Zero;
        private bool _useAirAbsorption = true;

        private IPL.AudioBuffer _iplInputBuffer;
        private IPL.AudioBuffer _iplSimulationBuffer;
        private IPL.AudioBuffer _iplOutputBuffer;

        private IPL.BinauralEffect _iplBinauralEffect;
        private IPL.DirectEffect _iplDirectEffect;
        private IPL.SimulationInputs _iplSimulationInputs;

        private readonly IntPtr _inBuffer = Marshal.AllocHGlobal(AudioManager.ipl_buffer_size_bytes);
        private readonly IntPtr _outBuffer = Marshal.AllocHGlobal(AudioManager.ipl_buffer_size_bytes * AudioManager.output_channels);

        public Sound(string path, IPL.Source iplSource, IPL.Context iplContext, IPL.Hrtf iplHrtf)
        {
            _iplContext = iplContext;

            if (!File.Exists(path))
            {
                return;
            }

            var file = File.ReadAllBytes(path);

            var sample = Bass.SampleLoad(file, 0, file.Length, 1, BassFlags.Decode | BassFlags.Float);
            var sampleData = Bass.SampleGetInfo(sample);

            var sampleBuffer = new byte[sampleData.Length];
            Bass.SampleGetData(sample, sampleBuffer);
            _audioStream = new MemoryStream(sampleBuffer);

            Bass.SampleFree(sample);

            _stream = Bass.CreateStream(AudioManager.sampling_rate, AudioManager.output_channels, BassFlags.Float, StreamProcedureType.Push);

            var iplAudioSettings = new IPL.AudioSettings
            {
                SamplingRate = AudioManager.sampling_rate,
                FrameSize = AudioManager.ipl_frame_size
            };

            IplRun(() => IPL.BinauralEffectCreate(_iplContext, iplAudioSettings, new IPL.BinauralEffectSettings { Hrtf = iplHrtf }, out _iplBinauralEffect));
            IplRun(() => IPL.DirectEffectCreate(_iplContext, iplAudioSettings, new IPL.DirectEffectSettings { NumChannels = 1 }, out _iplDirectEffect));

            IplRun(() => IPL.AudioBufferAllocate(_iplContext, 1, iplAudioSettings.FrameSize, ref _iplInputBuffer));
            IplRun(() => IPL.AudioBufferAllocate(_iplContext, 1, iplAudioSettings.FrameSize, ref _iplSimulationBuffer));
            IplRun(() => IPL.AudioBufferAllocate(_iplContext, AudioManager.output_channels, iplAudioSettings.FrameSize, ref _iplOutputBuffer));

            _iplSimulationInputs = new IPL.SimulationInputs
            {
                Flags = IPL.SimulationFlags.Direct,
                DirectFlags = IPL.DirectSimulationFlags.Directivity | IPL.DirectSimulationFlags.Occlusion | IPL.DirectSimulationFlags.Transmission,
                DistanceAttenuationModel = new IPL.DistanceAttenuationModel { Type = IPL.DistanceAttenuationModelType.Default, MinDistance = 100 },
                AirAbsorptionModel = new IPL.AirAbsorptionModel { Type = IPL.AirAbsorptionModelType.Default },
                Directivity = new IPL.Directivity { DipoleWeight = 0.1f, DipolePower = 1.0f },
                OcclusionType = IPL.OcclusionType.Raycast,
                Baked = false,
                Source = new IPL.CoordinateSpace3
                {
                    Ahead = (-Vector3.UnitZ).ToIplVector(),
                    Right = Vector3.UnitX.ToIplVector(),
                    Up = Vector3.UnitY.ToIplVector(),
                    Origin = Position.ToIplVector()
                }
            };

            IPL.SourceSetInputs(iplSource, IPL.SimulationFlags.Direct, _iplSimulationInputs);

            Source = iplSource;
        }

        public void Play()
        {
            if (_stream != null && !Playing)
            {
                _audioStream!.Position = 0;
                Bass.ChannelSetPosition(_stream.Value, 0);
                Playing = true;
                Bass.ChannelPlay(_stream.Value, true);
            }
        }

        public void Stop()
        {
            if (_stream != null && Playing)
            {
                Playing = false;
                Bass.ChannelStop(_stream.Value);
            }
        }

        private int QueuedBytes()
        {
            var n = Bass.ChannelGetData(_stream!.Value, IntPtr.Zero, (int)DataFlags.Available);
            return n < 0 ? 0 : n;
        }

        public unsafe void Update(IPL.Context iplContext, IPL.Hrtf iplHrtf)
        {
            if (_stream == null || !Playing)
                return;

            Bass.ChannelSetAttribute(_stream.Value, ChannelAttribute.Volume, Volume);

            const int outFrameBytes = AudioManager.ipl_buffer_size_bytes * AudioManager.output_channels;
            const int bufferFrames = 8;
            
            for (var i = 0; i < 4 && QueuedBytes() < outFrameBytes * bufferFrames; i++)
            {
                var inputSpan = new Span<byte>((void*)_inBuffer, AudioManager.ipl_buffer_size_bytes);
                var bytesRead = _audioStream!.Read(inputSpan);
                if (bytesRead == 0)
                {
                    Bass.StreamPutData(_stream.Value, nint.Zero, (int)StreamProcedureType.End);
                    Playing = false;
                    return;
                }

                // IPL needs a full frame — pad the tail with silence
                if (bytesRead < inputSpan.Length)
                    inputSpan[bytesRead..].Clear();

                var camera = Engine.MainViewport;

                var direction = IPL.CalculateRelativeDirection(iplContext,
                    Position.ToIplVector(),
                    camera.Position.ToIplVector(),
                    camera.Front.ToIplVector(),
                    camera.Up.ToIplVector());

                var binauralEffectParams = new IPL.BinauralEffectParams
                {
                    Hrtf = iplHrtf,
                    Direction = direction,
                    Interpolation = IPL.HrtfInterpolation.Nearest,
                    SpatialBlend = 1.0f,
                };

                IPL.AudioBufferDeinterleave(iplContext, Unsafe.AsRef<float>((float*)_inBuffer), _iplInputBuffer);

                IPL.SourceGetOutputs(Source, IPL.SimulationFlags.Direct, out var iplSourceOutput);

                // todo: can't do occlusion for now because we translate meshes in shaders
                // todo: figure out why distance attenuation makes all sounds very quiet
                if (_useAirAbsorption)
                    iplSourceOutput.Direct.Flags |= IPL.DirectEffectFlags.ApplyAirAbsorption;

                iplSourceOutput.Direct.Flags |= IPL.DirectEffectFlags.ApplyDirectivity |
                                                IPL.DirectEffectFlags.ApplyOcclusion |
                                                IPL.DirectEffectFlags.ApplyTransmission;

                IPL.DirectEffectApply(_iplDirectEffect, ref iplSourceOutput.Direct, ref _iplInputBuffer,
                    ref _iplSimulationBuffer);
                IPL.BinauralEffectApply(_iplBinauralEffect, ref binauralEffectParams, ref _iplSimulationBuffer,
                    ref _iplOutputBuffer);
                IPL.AudioBufferInterleave(iplContext, _iplOutputBuffer, Unsafe.AsRef<float>((float*)_outBuffer));

                var result = Bass.StreamPutData(_stream.Value, _outBuffer, outFrameBytes);
                if (result == -1)
                {
                    if (Bass.LastError == Errors.Ended)
                    {
                        Playing = false;
                        return;
                    }

                    Log.Context(this).Warning("BASS StreamPutData error {Error}", Bass.LastError);
                    return;
                }

                if (ConVarStorage.Get<bool>("audio_debug"))
                {
                    DebugRender.DrawText(_position, $"Input stream position:\t{_audioStream.Position}\n" +
                                                    $"Queued data: {result}\n" +
                                                    $"Output stream position: {Bass.ChannelGetPosition(_stream.Value) / AudioManager.output_channels}");
                }
            }
        }

        private static void IplRun(Func<IPL.Error> func)
        {
            var result = func();
            if (result != IPL.Error.Success)
            {
                Log.Context("IPL").Warning("IPL error {Error}", result);
            }
        }

        public void Dispose()
        {
            if (Playing)
                Stop();

            IPL.AudioBufferFree(_iplContext, ref _iplInputBuffer);
            IPL.AudioBufferFree(_iplContext, ref _iplSimulationBuffer);
            IPL.AudioBufferFree(_iplContext, ref _iplOutputBuffer);

            Marshal.FreeHGlobal(_inBuffer);
            Marshal.FreeHGlobal(_outBuffer);
            _audioStream?.Dispose();
        }
    }
}