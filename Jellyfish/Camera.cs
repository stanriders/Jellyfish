using System;
using OpenTK;

namespace Jellyfish
{
    public class Camera
    {
        private static Vector3 front = -Vector3.UnitZ;
        private static Vector3 up = Vector3.UnitY;
        private static Vector3 right = Vector3.UnitX;

        private static float pitch;
        private static float yaw = -MathHelper.PiOver2; // Without this you would be started rotated 90 degrees right
        private static float fov = MathHelper.PiOver2;

        public Camera(Vector3 position, float aspectRatio)
        {
            Position = position;
            AspectRatio = aspectRatio;
        }

        // The position of the camera
        public static Vector3 Position { get; set; }

        public static float AspectRatio { private get; set; }

        public static Vector3 Front => front;
        public static Vector3 Up => up;
        public static Vector3 Right => right;

        public float Pitch
        {
            get => MathHelper.RadiansToDegrees(pitch);
            set
            {
                // We clamp the pitch value between -89 and 89 to prevent the camera from going upside down
                var angle = MathHelper.Clamp(value, -89f, 89f); 
                pitch = MathHelper.DegreesToRadians(angle);
                UpdateVectors();
            }
        }

        public float Yaw
        {
            get => MathHelper.RadiansToDegrees(yaw);
            set
            {
                yaw = MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        public float Fov
        {
            get => MathHelper.RadiansToDegrees(fov);
            set
            {
                var angle = MathHelper.Clamp(value, 1f, 45f);
                fov = MathHelper.DegreesToRadians(angle);
            }
        }

        public static Matrix4 GetViewMatrix() => Matrix4.LookAt(Position, Position + front, up);

        public static Matrix4 GetProjectionMatrix() => Matrix4.CreatePerspectiveFieldOfView(fov, AspectRatio, 0.01f, 100f);

        private void UpdateVectors()
        {
            front.X = (float)Math.Cos(pitch) * (float)Math.Cos(yaw);
            front.Y = (float)Math.Sin(pitch);
            front.Z = (float)Math.Cos(pitch) * (float)Math.Sin(yaw);

            front = Vector3.Normalize(front);
            right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
            up = Vector3.Normalize(Vector3.Cross(right, front));
        }
    }
}
