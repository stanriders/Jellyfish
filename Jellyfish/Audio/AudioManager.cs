using Jellyfish.Entities;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfish.Console;
using Jellyfish.Render;
using SteamAudio;
using Vector3 = OpenTK.Mathematics.Vector3;

namespace Jellyfish.Audio
{
    public unsafe class AudioManager
    {
        public static float Volume { get; set; } = 1.0f;

        private IPL.Context _iplContext;
        private IPL.Hrtf _iplHrtf;
        private IPL.Scene _iplScene;
        private IPL.Simulator _iplSimulator;
        private static AudioManager? instance;

        private readonly List<Sound> _sounds = new();

        private bool _shouldStop;

        public const int output_channels = 2;
        public const int sampling_rate = 44100;

        public const int ipl_frame_size = 512;
        public const int ipl_buffer_size_bytes = ipl_frame_size * sizeof(float);
        public const int update_rate = (int)(ipl_frame_size / (double)sampling_rate * 1000);
        
        public AudioManager()
        {
            Bass.Init();
            Bass.UpdatePeriod = 0;

            var audioThread = new Thread(Run) { Name = "Audio thread" };
            audioThread.Start();

            instance = this;
        }

        public static Sound? AddSound(string path)
        {
            if (instance == null)
                return null;

            IPL.Source source = default;

            IplRun(() => IPL.SourceCreate(instance._iplSimulator, new IPL.SourceSettings { Flags = IPL.SimulationFlags.Direct }, out source));
            if (source != default)
            {
                var sound = new Sound(path, source, instance._iplContext, instance._iplHrtf);
                IPL.SourceAdd(source, instance._iplSimulator);
                IPL.SimulatorCommit(instance._iplSimulator);

                instance._sounds.Add(sound);

                return sound;
            }

            return null;
        }
        
        public static void AddMesh(MeshPart mesh)
        {
            if (instance == null)
                return;

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

            fixed (IPL.Vector3* verts = mesh.Vertices.Select(v => v.Coordinates.ToIplVector()).ToArray())
            fixed (IPL.Triangle* indicies = triangles.ToArray())
            fixed (IPL.Material* materials = materialsList)
            fixed (int* materialIndicies = Enumerable.Repeat(0, triangles.Count).ToArray())
            {
                IPL.StaticMeshCreate(instance._iplScene, new IPL.StaticMeshSettings
                {
                    NumVertices = mesh.Vertices.Count,
                    NumTriangles = triangles.Count,
                    NumMaterials = materialsList.Length,
                    Vertices = (nint)verts,
                    Triangles = (nint)indicies,
                    MaterialIndices = (nint)materialIndicies,
                    Materials = (nint)materials
                }, out var iplMesh);

                IPL.StaticMeshAdd(iplMesh, instance._iplScene);
                IPL.SceneCommit(instance._iplScene);
            }
        }

        public void Unload()
        {
            _shouldStop = true;
        }

        private void Run()
        {
            Log.Context(this).Information("Starting audio thread...");

            Volume = Settings.Instance.Audio.Volume;

            var contextCreateResult = IPL.ContextCreate(new IPL.ContextSettings { Version = IPL.Version }, out _iplContext);
            if (contextCreateResult != IPL.Error.Success)
            {
                Log.Context(this).Error("Couldn't start SteamAudio! {Error}", contextCreateResult);
            }

            var iplAudioSettings = new IPL.AudioSettings
            {
                SamplingRate = sampling_rate,
                FrameSize = ipl_frame_size
            };

            var hrtfSettings = new IPL.HrtfSettings
            {
                Type = IPL.HrtfType.Default,
                Volume = 1.0f,
                NormType = IPL.HrtfNormType.None
            };

            IplRun(() => IPL.HrtfCreate(_iplContext, in iplAudioSettings, hrtfSettings, out _iplHrtf));

            IplRun(() => IPL.SimulatorCreate(_iplContext, new IPL.SimulationSettings
            {
                Flags = IPL.SimulationFlags.Direct,
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

            Log.Context(this).Information("SteamAudio is ready.");

            while (!_shouldStop)
            {
                Thread.Sleep(update_rate);
                Bass.Update(update_rate);
                Bass.GlobalStreamVolume = (int)(Volume * 10000);

                var error = Bass.LastError;
                if (error != Errors.OK)
                {
                    Log.Context(this).Warning("BASS error {Error}", error);
                }

                if (_sounds.Count(x=> x.Playing) == 0)
                    continue;

                var camera = Camera.Instance;
                if (camera == null)
                    return;

                var cameraPosition = camera.GetPropertyValue<Vector3>("Position");

                var listener = new IPL.CoordinateSpace3
                {
                    Ahead = camera.Front.ToIplVector(),
                    Up = camera.Up.ToIplVector(),
                    Right = camera.Right.ToIplVector(),
                    Origin = cameraPosition.ToIplVector()
                };
                
                IPL.SimulatorSetSharedInputs(_iplSimulator, IPL.SimulationFlags.Direct, new IPL.SimulationSharedInputs
                {
                    Listener = listener,
                    NumRays = 2048,
                    IrradianceMinDistance = 100.0f,
                    Duration = 2.0f,
                    Order = 1,
                    NumBounces = 1
                });

                IPL.SimulatorRunDirect(_iplSimulator);

                foreach (var sound in _sounds)
                {
                    sound.Update(_iplContext, _iplHrtf);
                }
            }

            IPL.ContextRelease(ref _iplContext);
            Bass.Free();
        }

        private static void IplRun(Func<IPL.Error> func)
        {
            var result = func();
            if (result != IPL.Error.Success)
            {
                Log.Context("IPL").Warning("IPL error {Error}", result);
            }
        }
    }

}
