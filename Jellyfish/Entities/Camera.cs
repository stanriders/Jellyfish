using System;
using Jellyfish.Console;
using Jellyfish.Input;
using JoltPhysicsSharp;
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

    private bool _noclip;

    private readonly Spotlight? _camLight;
    private readonly CharacterVirtual? _physCharacter;

    private const float camera_speed = 120.0f;
    private const float jump_velocity = 250.0f;
    private const float sensitivity = 0.2f;

    public bool IsControllingCursor { get; set; }

    private static Camera? camera;
    public static Camera? Instance
    {
        get
        {
            if (camera != null) 
                return camera;

            camera = EntityManager.FindEntity("camera") as Camera;
            if (camera == null)
            {
                Log.Context("Camera").Error("Camera doesn't exist!");
                return null;
            }
            return camera;
        }
    }

    public Camera()
    {
        SetPropertyValue("Name", "cam");
        if (_camLight is null)
        {
            _camLight = EntityManager.CreateEntity("light_spot") as Spotlight;
            if (_camLight != null)
            {
#if DEBUG
                _camLight.DrawDevCone = false;
#endif
                _camLight.SetPropertyValue("Name", "cam light");
                _camLight.SetPropertyValue("Enabled", true);
                _camLight.SetPropertyValue("Quadratic", 0.01f);
                _camLight.SetPropertyValue("Linear", 0.8f);
                _camLight.SetPropertyValue("Constant", 0.2f);
                _camLight.SetPropertyValue("Color", new Color4<Rgba>(200 / 255.0f, 220 / 255.0f, 1f, 10 / 255.0f));
                _camLight.SetPropertyValue("OuterCone", 60f);
                _camLight.SetPropertyValue("Cone", 40f);
                _camLight.Load();
            }
        }
        
        _physCharacter = PhysicsManager.AddPlayerController(this);
        InputManager.RegisterInputHandler(this);

        base.Load();
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
            var angle = Math.Clamp(value, -89.9f, 89.9f);
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
            var angle = Math.Clamp(value, 1f, 45f);
            _fov = MathHelper.DegreesToRadians(angle);
        }
    }

    public Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(GetPropertyValue<Vector3>("Position"), GetPropertyValue<Vector3>("Position") + _front, Up);
    }

    public Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 0.05f, 10000f);
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

        var quatRotation = new Matrix3(_front, Up, Right).ExtractRotation();
        var lightRotation = new Matrix3(_front, Up, Right).Inverted().ExtractRotation();

        if (_camLight is not null)
        {
            _camLight.SetPropertyValue("Position", GetPropertyValue<Vector3>("Position"));
            _camLight.SetPropertyValue("Rotation", lightRotation);
        }

        SetPropertyValue("Rotation", quatRotation);
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        var inputHandled = false;

        if (keyboardState.IsKeyPressed(Keys.L))
        {
            if (_camLight is not null)
            {
                var isEnabled = _camLight.GetPropertyValue<bool>("Enabled");
                _camLight.SetPropertyValue("Enabled", !isEnabled);
                inputHandled = true;
            }
        }

        if (keyboardState.IsKeyPressed(Keys.V))
        {
            _noclip = !_noclip;
            inputHandled = true;
        }
        
        var cameraSpeed = keyboardState.IsKeyDown(Keys.LeftShift) ? camera_speed * 4 : camera_speed;

        if (_physCharacter != null && !_noclip)
        {
            SetPropertyValue("Position", _physCharacter.Position.ToOpentkVector());

            bool playerControlsHorizontalVelocity = _physCharacter.IsSupported;

            var verticalVelocity = (Vector3.Dot(_physCharacter.LinearVelocity.ToOpentkVector(), Vector3.UnitY) * Vector3.UnitY).ToNumericsVector();

            var desiredVelocity = new System.Numerics.Vector3();
            var newVelocity = verticalVelocity;

            if (_physCharacter.GroundState != GroundState.InAir)
            {
                newVelocity = _physCharacter.GroundVelocity;
            }

            if (mouseState.IsButtonDown(MouseButton.Left))
            {
                if (keyboardState.IsKeyDown(Keys.W))
                    desiredVelocity += new System.Numerics.Vector3(Front.X, 0, Front.Z) * cameraSpeed;  // Forward 
                if (keyboardState.IsKeyDown(Keys.S))
                    desiredVelocity -= new System.Numerics.Vector3(Front.X, 0, Front.Z) * cameraSpeed; // Backwards
                if (keyboardState.IsKeyDown(Keys.A))
                    desiredVelocity -= new System.Numerics.Vector3(Right.X, 0, Right.Z) * cameraSpeed; // Left
                if (keyboardState.IsKeyDown(Keys.D))
                    desiredVelocity += new System.Numerics.Vector3(Right.X, 0, Right.Z) * cameraSpeed; // Right

                // jump
                if (keyboardState.IsKeyDown(Keys.Space))
                    desiredVelocity += System.Numerics.Vector3.UnitY * jump_velocity;

                Yaw += mouseState.Delta.X * sensitivity;
                Pitch -= mouseState.Delta.Y * sensitivity;

                inputHandled = true;

                if (!IsControllingCursor)
                {
                    InputManager.CaptureInput(this);
                    IsControllingCursor = true;
                }
            }

            newVelocity += (System.Numerics.Vector3.UnitY * PhysicsManager.GetGravity()) * frameTime;

            if (playerControlsHorizontalVelocity)
            {
                newVelocity += desiredVelocity;
            }
            else
            {
                // Preserve horizontal velocity
                var currentHorizontalVelocity = _physCharacter.LinearVelocity - verticalVelocity;
                newVelocity += currentHorizontalVelocity;
            }

            _physCharacter.LinearVelocity = newVelocity;
        }
        else
        {
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
                {
                    InputManager.CaptureInput(this);
                    IsControllingCursor = true;
                }

                inputHandled = true;
            }
        }

        if (IsControllingCursor && !inputHandled)
        {
            InputManager.ReleaseInput(this);
            IsControllingCursor = false;
        }

        return inputHandled;
    }
}