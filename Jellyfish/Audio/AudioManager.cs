using Jellyfish.Entities;
using ManagedBass;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Jellyfish.Render;
using IPL = Jellyfish.Audio.SteamAudio.IPL;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace Jellyfish.Audio
{
    public unsafe class AudioManager
    {
        private IPL.Context _iplContext;
        private IPL.Hrtf _iplHrtf;
        private IPL.BinauralEffect _iplBinauralEffect;
        private IPL.AudioBuffer _iplInputBuffer;
        private IPL.AudioBuffer _iplSimulationBuffer;
        private IPL.AudioBuffer _iplOutputBuffer;
        private static IPL.Scene _iplScene;
        private static IPL.Simulator _iplSimulator;
        private IPL.DirectEffect _iplDirectEffect;

        private readonly IntPtr _inBuffer = Marshal.AllocHGlobal(ipl_buffer_size_bytes);
        private readonly IntPtr _outBuffer = Marshal.AllocHGlobal(ipl_buffer_size_bytes * output_channels);

        private bool _shouldStop;

        private const int output_channels = 2;
        private const int sampling_rate = 44100;

        private const int ipl_frame_size = 512;
        private const int ipl_buffer_size_bytes = ipl_frame_size * sizeof(float);
        private const int update_rate = (int)(ipl_frame_size / (double)sampling_rate * 1000); // it's not just buffer_size because of all the int castings

        // TODO: add mixer and support more than one sound
        private static Vector3 _soundPosition;
        private static MemoryStream? _audioStream;
        private bool _playing;
        private static int _stream;
        private static IPL.Source _source;

        public AudioManager()
        {
            Bass.Init();
            Bass.UpdatePeriod = 0;

            Bass.GlobalStreamVolume = 10000;
            Bass.Volume = 1;

            var audioThread = new Thread(Run) { Name = "Audio thread" };
            audioThread.Start();
        }

        public static int Play(string path, Vector3 position)
        {
            var file = File.ReadAllBytes(path);

            var sample = Bass.SampleLoad(file, 0, file.Length, 1, BassFlags.Decode | BassFlags.Float);
            var sampleData = Bass.SampleGetInfo(sample);
            var sampleBuffer = new byte[sampleData.Length];
            Bass.SampleGetData(sample, sampleBuffer);
            _audioStream = new MemoryStream(sampleBuffer);

            Bass.SampleFree(sample);

            _stream = Bass.CreateStream(sampling_rate, output_channels, BassFlags.Float, StreamProcedureType.Push);
            _soundPosition = position;

            IPL.SourceCreate(_iplSimulator, new IPL.SourceSettings { Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections }, out _source);
            IPL.SourceSetInputs(_source, IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections, new IPL.SimulationInputs
            {
                Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections,
                DirectFlags = IPL.DirectSimulationFlags.Occlusion | IPL.DirectSimulationFlags.Transmission | IPL.DirectSimulationFlags.DistanceAttenuation | IPL.DirectSimulationFlags.Directivity | IPL.DirectSimulationFlags.AirAbsorption,
                DistanceAttenuationModel = new IPL.DistanceAttenuationModel { Type = IPL.DistanceAttenuationModelType.InverseDistance, MinDistance = 50 },
                AirAbsorptionModel = new IPL.AirAbsorptionModel {Type = IPL.AirAbsorptionModelType.Default },
                Directivity = new IPL.Directivity { DipoleWeight = 0.1f, DipolePower = 1.0f },
                OcclusionType = IPL.OcclusionType.Raycast,
                Baked = false,
                Source = new IPL.CoordinateSpace3
                {
                    Ahead = new IPL.Vector3(-Vector3.UnitZ.X, -Vector3.UnitZ.Y, -Vector3.UnitZ.Z), 
                    Right = new IPL.Vector3(Vector3.UnitX.X, Vector3.UnitX.Y, Vector3.UnitX.Z), 
                    Up = new IPL.Vector3(Vector3.UnitY.X, Vector3.UnitY.Y, Vector3.UnitY.Z),
                    Origin = new IPL.Vector3(_soundPosition.X, _soundPosition.Y, _soundPosition.Z)
                }
            });
            IPL.SourceAdd(_source, _iplSimulator);

            IPL.SimulatorCommit(_iplSimulator);
            return _stream;
        }

        public static void Update(int channel, Vector3 position)
        {
            _soundPosition = position;

            // todo: can we update just the pos?
            IPL.SourceSetInputs(_source, IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections, new IPL.SimulationInputs()
            {
                Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections,
                DirectFlags = IPL.DirectSimulationFlags.Occlusion | IPL.DirectSimulationFlags.Transmission | IPL.DirectSimulationFlags.DistanceAttenuation | IPL.DirectSimulationFlags.Directivity | IPL.DirectSimulationFlags.AirAbsorption,
                DistanceAttenuationModel = new IPL.DistanceAttenuationModel {Type = IPL.DistanceAttenuationModelType.InverseDistance, MinDistance = 50},
                Directivity = new IPL.Directivity { DipoleWeight = 0.1f, DipolePower = 1.0f},
                AirAbsorptionModel = new IPL.AirAbsorptionModel { Type = IPL.AirAbsorptionModelType.Default },
                OcclusionType = IPL.OcclusionType.Raycast,
                Baked = false,
                Source = new IPL.CoordinateSpace3
                {
                    Ahead = new IPL.Vector3(-Vector3.UnitZ.X, -Vector3.UnitZ.Y, -Vector3.UnitZ.Z),
                    Right = new IPL.Vector3(Vector3.UnitX.X, Vector3.UnitX.Y, Vector3.UnitX.Z),
                    Up = new IPL.Vector3(Vector3.UnitY.X, Vector3.UnitY.Y, Vector3.UnitY.Z),
                    Origin = new IPL.Vector3(_soundPosition.X, _soundPosition.Y, _soundPosition.Z)
                }
            });
            IPL.SimulatorCommit(_iplSimulator);
        }

        public static void AddMesh(MeshInfo mesh)
        {
            var triangles = new List<IPL.Triangle>();
            for (var i = 0; i < mesh.Vertices.Count; i += 3)
            {
                var triangle = new IPL.Triangle();
                triangle.Indices[0] = i + 2;
                triangle.Indices[1] = i + 1;
                triangle.Indices[2] = i;
                triangles.Add(triangle); // todo: what winding are we actually using??????
            }

            // {"ceramic",{0.01f,0.02f,0.02f,0.05f,0.060f,0.044f,0.011f}}
            var material = new IPL.Material();
            material.Absorption[0] = 0.01f;
            material.Absorption[1] = 0.01f;
            material.Absorption[2] = 0.01f;

            material.Transmission[0] = 0.060f;
            material.Transmission[1] = 0.044f;
            material.Transmission[2] = 0.011f;

            material.Scattering = 0.05f;

            var materialsList = new[] { material };

            fixed (IPL.Vector3* verts = mesh.Vertices.Select(v => new IPL.Vector3(v.X, v.Y, v.Z)).ToArray())
            fixed (IPL.Triangle* indicies = triangles.ToArray())
            fixed (IPL.Material* materials = materialsList)
            fixed (int* materialIndicies = Enumerable.Repeat(0, triangles.Count).ToArray())
            {
                IPL.StaticMeshCreate(_iplScene, new IPL.StaticMeshSettings
                {
                    NumVertices = mesh.Vertices.Count,
                    NumTriangles = triangles.Count,
                    NumMaterials = materialsList.Length,
                    Vertices = (nint)verts,
                    Triangles = (nint)indicies,
                    MaterialIndices = (nint)materialIndicies,
                    Materials = (nint)materials
                }, out var iplMesh);

                IPL.StaticMeshAdd(iplMesh, _iplScene);
                IPL.SceneCommit(_iplScene);
            }
        }

        public void Unload()
        {
            _shouldStop = true;
        }

        private void Run()
        {
            Log.Information("[AudioManager] Starting audio thread...");

            var contextCreateResult = IPL.ContextCreate(new IPL.ContextSettings { Version = IPL.Version }, out _iplContext);
            if (contextCreateResult != IPL.Error.Success)
            {
                Log.Error("[AudioManager] Couldn't start SteamAudio! {Error}", contextCreateResult);
            }

            var iplAudioSettings = new IPL.AudioSettings
            {
                SamplingRate = sampling_rate,
                FrameSize = ipl_frame_size
            };

            IplRun(() => IPL.HrtfCreate(_iplContext, in iplAudioSettings, new IPL.HrtfSettings { Type = IPL.HrtfType.Default }, out _iplHrtf));
            IplRun(() => IPL.BinauralEffectCreate(_iplContext, in iplAudioSettings, new IPL.BinauralEffectSettings { Hrtf = _iplHrtf }, out _iplBinauralEffect));
            IplRun(() => IPL.DirectEffectCreate(_iplContext, iplAudioSettings, new IPL.DirectEffectSettings { NumChannels = 1 }, out _iplDirectEffect));

            IplRun(() => IPL.AudioBufferAllocate(_iplContext, 1, iplAudioSettings.FrameSize, ref _iplInputBuffer));
            IplRun(() => IPL.AudioBufferAllocate(_iplContext, 1, iplAudioSettings.FrameSize, ref _iplSimulationBuffer));
            IplRun(() => IPL.AudioBufferAllocate(_iplContext, output_channels, iplAudioSettings.FrameSize, ref _iplOutputBuffer));
            
            IplRun(() => IPL.SimulatorCreate(_iplContext, new IPL.SimulationSettings
            {
                Flags = IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections, // reflections must be enabled due to a bug in 4.0 version of steamaudio (https://github.com/ValveSoftware/steam-audio/issues/190)
                SceneType = IPL.SceneType.Default,
                ReflectionType = IPL.ReflectionEffectType.Parametric,
                FrameSize = ipl_frame_size,
                SamplingRate = sampling_rate,
                MaxNumRays = 4096,
                NumDiffuseSamples = 32,
                MaxDuration = 2.0f,
                MaxOrder = 1,
                MaxNumSources = 8,
                NumThreads = 1,
            }, out _iplSimulator));

            IplRun(() => IPL.SceneCreate(_iplContext, new IPL.SceneSettings { Type = IPL.SceneType.Default }, out _iplScene));

            IPL.SimulatorSetScene(_iplSimulator, _iplScene);
            IPL.SimulatorCommit(_iplSimulator);

            Log.Information("[AudioManager] SteamAudio is ready.");

            while (!_shouldStop)
            {
                Bass.Update(update_rate);

                var error = Bass.LastError;
                if (error != Errors.OK)
                {
                    Log.Warning("[AudioManager] BASS error {Error}", error);
                }

                if (_stream == 0 || _audioStream == null)
                    continue;

                var camera = EntityManager.FindEntity("camera") as Camera;
                if (camera == null)
                {
                    Log.Error("Camera doesn't exist!");
                    continue;
                }

                var cameraPosition = camera.GetPropertyValue<Vector3>("Position");

                var listener = new IPL.CoordinateSpace3
                {
                    Ahead = new IPL.Vector3(camera.Front.X, camera.Front.Y, camera.Front.Z),
                    Up = new IPL.Vector3(camera.Up.X, camera.Up.Y, camera.Up.Z),
                    Right = new IPL.Vector3(camera.Right.X, camera.Right.Y, camera.Right.Z),
                    Origin = new IPL.Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z)
                };

                var direction = IPL.CalculateRelativeDirection(_iplContext,
                    new IPL.Vector3(_soundPosition.X, _soundPosition.Y, _soundPosition.Z),
                    new IPL.Vector3(cameraPosition.X, cameraPosition.Y, cameraPosition.Z),
                    new IPL.Vector3(camera.Front.X, camera.Front.Y, camera.Front.Z),
                    new IPL.Vector3(camera.Up.X, camera.Up.Y, camera.Up.Z));

                var binauralEffectParams = new IPL.BinauralEffectParams
                {
                    Hrtf = _iplHrtf,
                    Direction = direction,
                    Interpolation = IPL.HrtfInterpolation.Nearest,
                    SpatialBlend = 1.0f,
                };

                var inputBufferByteSpan = new Span<byte>((void*)_inBuffer, ipl_buffer_size_bytes);
                int bytesRead = _audioStream.Read(inputBufferByteSpan);
                if (bytesRead == 0)
                {
                    if (_playing)
                    {
                        //Bass.StreamPutData(_stream, nint.Zero, (int)StreamProcedureType.End);
                        //_playing = false;
                        _audioStream.Position = 0;
                    }

                    continue;
                }

                IPL.AudioBufferDeinterleave(_iplContext, Unsafe.AsRef<float>((float*)_inBuffer), _iplInputBuffer);

                IPL.SimulatorSetSharedInputs(_iplSimulator, IPL.SimulationFlags.Direct, new IPL.SimulationSharedInputs
                {
                    Listener = listener,
                    NumRays = 2048,
                    IrradianceMinDistance = 1.0f,
                    Duration = 2.0f,
                    Order = 1,
                    NumBounces = 1
                });

                IPL.SimulatorRunDirect(_iplSimulator);

                IPL.SourceGetOutputs(_source, IPL.SimulationFlags.Direct | IPL.SimulationFlags.Reflections, out var iplSourceOutput);

                // todo: can't do occlusion for now because we translate meshes in shaders
                iplSourceOutput.Direct.Flags = IPL.DirectEffectFlags.ApplyAirAbsorption | IPL.DirectEffectFlags.ApplyDirectivity;
                
                IPL.DirectEffectApply(_iplDirectEffect, ref iplSourceOutput.Direct, ref _iplInputBuffer,
                    ref _iplSimulationBuffer);

                IPL.BinauralEffectApply(_iplBinauralEffect, ref binauralEffectParams, ref _iplSimulationBuffer,
                    ref _iplOutputBuffer);
                
                IPL.AudioBufferInterleave(_iplContext, _iplOutputBuffer, Unsafe.AsRef<float>((float*)_outBuffer));

                var result = Bass.StreamPutData(_stream, _outBuffer, ipl_buffer_size_bytes * output_channels);
                if (result == -1)
                {
                    Log.Warning("[AudioManager] BASS StreamPutData error {Error}", Bass.LastError);
                }

                if (!_playing)
                {
                    if (!Bass.ChannelPlay(_stream))
                    {
                        Log.Warning("[AudioManager] BASS ChannelPlay error {Error}", Bass.LastError);
                    }
                    else
                    {
                        _playing = true;
                    }
                }

                Thread.Sleep(update_rate);
            }

            IPL.ContextRelease(ref _iplContext);
            Bass.Free();
        }

        private static void IplRun(Func<IPL.Error> func)
        {
            var result = func();
            if (result != IPL.Error.Success)
            {
                Log.Warning("[AudioManager] BASS IPL error {Error}", result);
            }
        }
    }

}
