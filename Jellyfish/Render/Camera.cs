using OpenTK.Mathematics;
using System;
using Jellyfish.Utils;

namespace Jellyfish.Render
{
    public class Camera
    {
        private Vector3 _front = -Vector3.UnitZ;

        private float _pitch;
        private float _yaw = -MathHelper.PiOver2; // Without this you would be started rotated 90 degrees right
        private float _fov = MathHelper.PiOver2;

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; }
        public float AspectRatio { get; set; }

        public Vector3 Front => _front;
        public Vector3 Up { get; set; } = Vector3.UnitY;
        public Vector3 Right { get; set; } = Vector3.UnitX;

        public bool IsControllingCursor { get; set; }

        private static Camera? camera;
        public static Camera Instance
        {
            get
            {
                if (camera != null)
                    return camera;

                camera = new Camera();
                return camera;
            }
            set => camera = value;
        }

        public float Pitch
        {
            get => MathHelper.RadiansToDegrees(_pitch);
            set
            {
                // We clamp the pitch value between -89 and 89 to prevent the camera from going upside down
                var angle = Math.Clamp(value, -89.9f, 89.9f);
                _pitch = MathHelper.DegreesToRadians(angle);
                UpdateVectors();
            }
        }

        public float Yaw
        {
            get => MathHelper.RadiansToDegrees(_yaw);
            set
            {
                if (value > 180.0f)
                    value = -180.0f;
                else if (value < -180.0f)
                    value = 180.0f;

                _yaw = MathHelper.DegreesToRadians(value);
                UpdateVectors();
            }
        }

        public float Fov
        {
            get => MathHelper.RadiansToDegrees(_fov);
            set
            {
                var angle = Math.Clamp(value, 1f, 45f);
                _fov = MathHelper.DegreesToRadians(angle);
            }
        }

        public static float NearPlane => 1f;
        public static float FarPlane => 20000f;

        public Camera()
        {
            camera = this;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + _front, Up);
        }

        public Matrix4 GetProjectionMatrix(float? nearPlane = null, float? farPlane = null)
        {
            return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, nearPlane ?? NearPlane, farPlane ?? FarPlane);
        }

        public Frustum GetFrustum(float? nearPlane = null, float? farPlane = null)
        {
            return new Frustum(GetViewMatrix() * GetProjectionMatrix(nearPlane, farPlane));
        }

        public Ray GetCameraToViewportRay(Vector2 screenPosition)
        {
            var inverseVp = (GetViewMatrix() * GetProjectionMatrix()).Inverted();

            var ndc = new Vector2(2.0f * screenPosition.X - 1.0f, 2.0f * (1.0f - screenPosition.Y) - 1f);
            var clip = new Vector4(ndc, -1.0f, 1.0f);

            var view = clip * inverseVp;
            view /= view.W;

            var nearPointWorld = new Vector3(view.X, view.Y, view.Z);
            var rayOrigin = Position;
            var rayDirection = Vector3.Normalize(nearPointWorld - rayOrigin);

            return new Ray(rayOrigin, rayDirection);
        }

        public void Think()
        {
            UpdateVectors();
        }

        private void UpdateVectors()
        {
            _front.X = (float)Math.Cos(_pitch) * (float)Math.Cos(_yaw);
            _front.Y = (float)Math.Sin(_pitch);
            _front.Z = (float)Math.Cos(_pitch) * (float)Math.Sin(_yaw);

            _front = Vector3.Normalize(_front);
            Right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, _front));

            var quatRotation = new Matrix3(_front, Up, Right).ExtractRotation();
            Rotation = quatRotation;
        }
    }
}
