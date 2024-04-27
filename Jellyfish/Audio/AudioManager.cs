using System.IO;
using Jellyfish.Entities;
using ManagedBass;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Audio
{
    public class AudioManager
    {
        private Vector3 _prevCameraPosition = Vector3.Zero;

        public AudioManager()
        {
            Bass.Init(Flags: DeviceInitFlags.Device3D);
            Bass.UpdatePeriod = 0;
        }

        public static int Play(string path, Vector3 position)
        {
            var file = File.ReadAllBytes(path);
            var sample = Bass.SampleLoad(file, 0, file.Length, 1, BassFlags.Bass3D);
            var channel = Bass.SampleGetChannel(sample);
            Bass.ChannelSet3DPosition(channel, new Vector3D(-position.X, position.Y, position.Z), new Vector3D(0,0,0), new Vector3D(0,0,0));
            Bass.ChannelSet3DAttributes(channel, Mode3D.Normal, 20, -1, 360, 360, -1);
            Bass.Apply3D();

            Bass.ChannelPlay(channel);

            return channel;
        }

        public static void Update(int channel, Vector3 position, Vector3 velocity)
        {
            Bass.ChannelSet3DPosition(channel, 
                new Vector3D(-position.X, position.Y, position.Z), 
                new Vector3D(0, 0, 0), 
                new Vector3D(-velocity.X, velocity.Y, velocity.Z));

            Bass.Apply3D();
        }

        public void Update()
        {
            Bass.Update(20);

            var error = Bass.LastError;
            if (error != Errors.OK)
            {
                Log.Warning("[AudioManager] BASS error {Error}", error);
            }

            var camera = EntityManager.FindEntity("camera") as Camera;
            if (camera == null)
            {
                Log.Error("Camera doesn't exist!");
                return;
            }

            var position = camera.GetPropertyValue<Vector3>("Position");

            var velocity = position - _prevCameraPosition;

            Bass.Set3DPosition(new Vector3D(-position.X, position.Y, position.Z), 
                new Vector3D(-velocity.X, velocity.Y, velocity.Z), 
                new Vector3D(-camera.Front.X, camera.Front.Y, camera.Front.Z), 
                new Vector3D(-camera.Up.X, camera.Up.Y, camera.Up.Z));
            Bass.Apply3D();

            _prevCameraPosition = position;
        }
    }
}
