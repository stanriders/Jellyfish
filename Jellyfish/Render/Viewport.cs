using OpenTK.Mathematics;
using System;
using Jellyfish.Utils;

namespace Jellyfish.Render
{
    public class Viewport
    {
        private Vector3 _front = -Vector3.UnitZ;

        private float _pitch;
        private float _yaw = -MathHelper.PiOver2; // Without this you would be started rotated 90 degrees right
        private float _fov = MathHelper.PiOver2;

        private Vector3 _position;
        public Vector3 Position
        {
            get
            {
                if (ViewMatrixOverride != null)
                    return ViewMatrixOverride.Value.Inverted().ExtractTranslation();

                return _position;
            }
            set => _position = value;
        }
        public Quaternion Rotation { get; set; }
        public float AspectRatio => Size.X / (float)Size.Y;
        public Vector2i Size { get; set; }

        public Vector3 Front => _front;
        public Vector3 Up { get; set; } = Vector3.UnitY;
        public Vector3 Right { get; set; } = Vector3.UnitX;

        public Matrix4? ViewMatrixOverride { get; set; } = null;
        public Matrix4? ProjectionMatrixOverride { get; set; } = null;

        private Matrix4? _frameProjectionMatrix;
        private Matrix4? _frameViewMatrix;
        private Frustum? _frameFrustum;

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
            set => _fov = MathHelper.DegreesToRadians(value);
        }

        public float NearPlane { get; init; } = 1f;
        public float FarPlane { get; init; } = 20000f;

        public Matrix4 GetViewMatrix()
        {
            if (ViewMatrixOverride != null)
                return ViewMatrixOverride.Value;

            return _frameViewMatrix ??= Matrix4.LookAt(Position, Position + _front, Up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            if (ProjectionMatrixOverride != null)
                return ProjectionMatrixOverride.Value;

            return _frameProjectionMatrix ??= Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, NearPlane, FarPlane);
        }

        public Frustum GetFrustum()
        {
            if (ProjectionMatrixOverride != null || ViewMatrixOverride != null)
                return new Frustum(GetViewMatrix() * GetProjectionMatrix());

            return _frameFrustum ??= new Frustum(GetViewMatrix() * GetProjectionMatrix());
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
            // new frame - reset matrices 
            _frameProjectionMatrix = null;
            _frameViewMatrix = null;
            _frameFrustum = null;

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
