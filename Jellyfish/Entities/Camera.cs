using System;
using Jellyfish.Input;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Entities;

[Entity("camera")]
public class Camera : BaseEntity, IInputHandler
{
    private Vector3 _front = -Vector3.UnitZ;

    private float _pitch;
    private float _yaw = -MathHelper.PiOver2; // Without this you would be started rotated 90 degrees right
    private float _fov = MathHelper.PiOver2;

    private readonly PointLight? _camLight;
    private const float camera_speed = 32.0f;
    private const float sensitivity = 0.2f;

    public bool IsControllingCursor { get; set; }

    public Camera()
    {
        SetPropertyValue("Name", "cam");
        if (_camLight is null)
        {
            _camLight = EntityManager.CreateEntity("light_point") as PointLight;
            if (_camLight != null)
            {
                _camLight.SetPropertyValue("Name", "cam light");
                _camLight.SetPropertyValue("Enabled", true);
                _camLight.SetPropertyValue("Quadratic", 0.0f);
                _camLight.SetPropertyValue("Linear", 0.8f);
                _camLight.SetPropertyValue("Constant", 0.2f);
                _camLight.SetPropertyValue("Color", new Color4(200, 220, 255, 10));
                _camLight.Load();
            }
        }
        InputManager.RegisterInputHandler(this);
    }

    public float AspectRatio { get; set; }

    public Vector3 Front => _front;
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public Vector3 Right { get; set; } = Vector3.UnitX;

    private float Pitch
    {
        get => MathHelper.RadiansToDegrees(_pitch);
        set
        {
            // We clamp the pitch value between -89 and 89 to prevent the camera from going upside down
            var angle = MathHelper.Clamp(value, -89.9f, 89.9f);
            _pitch = MathHelper.DegreesToRadians(angle);
            UpdateVectors();
        }
    }

    private float Yaw
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
            var angle = MathHelper.Clamp(value, 1f, 45f);
            _fov = MathHelper.DegreesToRadians(angle);
        }
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(GetPropertyValue<Vector3>("Position"), GetPropertyValue<Vector3>("Position") + _front, Up);
    }

    public Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.05f, 1000f);
    }

    public override void Think()
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

        if (_camLight is not null)
        {
            _camLight.SetPropertyValue("Position", GetPropertyValue<Vector3>("Position"));
        }

        SetPropertyValue("Rotation", new Vector3(Pitch, Yaw, 0));
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (keyboardState.IsKeyPressed(Keys.L))
        {
            if (_camLight is not null)
            {
                var isEnabled = _camLight.GetPropertyValue<bool>("Enabled");
                _camLight.SetPropertyValue("Enabled", !isEnabled);
            }
        }

        var cameraSpeed = keyboardState.IsKeyDown(Keys.LeftShift) ? camera_speed * 4 : camera_speed;

        if (mouseState.IsButtonDown(MouseButton.Left))
        {
            var position = GetPropertyValue<Vector3>("Position");

            if (keyboardState.IsKeyDown(Keys.W))
                position += Front * cameraSpeed * frameTime; // Forward 
            if (keyboardState.IsKeyDown(Keys.S))
                position -= Front * cameraSpeed * frameTime; // Backwards
            if (keyboardState.IsKeyDown(Keys.A))
                position -= Right * cameraSpeed * frameTime; // Left
            if (keyboardState.IsKeyDown(Keys.D))
                position += Right * cameraSpeed * frameTime; // Right
            if (keyboardState.IsKeyDown(Keys.Space))
                position += Up * cameraSpeed * frameTime; // Up 
            if (keyboardState.IsKeyDown(Keys.LeftControl))
                position -= Up * cameraSpeed * frameTime; // Down

            SetPropertyValue("Position", position);

            Yaw += mouseState.Delta.X * sensitivity;
            Pitch -= mouseState.Delta.Y * sensitivity;

            if (!IsControllingCursor)
                IsControllingCursor = true;

            return true;
        }

        if (IsControllingCursor)
            IsControllingCursor = false;

        return false;
    }
}