using System;
using Jellyfish.Console;
using Jellyfish.Input;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Ray = Jellyfish.Utils.Ray;

namespace Jellyfish.Entities;

[Entity("player")]
public class Player : BaseEntity, IInputHandler
{
    private Vector3 _front = -Vector3.UnitZ;

    private float _pitch;
    private float _yaw = -MathHelper.PiOver2; // Without this you would be started rotated 90 degrees right
    private float _fov = MathHelper.PiOver2;

    private bool _noclip;

    private CharacterVirtual? _physCharacter;

    private const float camera_speed = 120.0f;
    private const float jump_velocity = 170.0f;
    private const float sensitivity = 0.2f;

    public bool IsControllingCursor { get; set; }

    private static Player? player;
    public static Player? Instance
    {
        get
        {
            if (player != null) 
                return player;

            player = EntityManager.FindEntity("player", true) as Player;
            return player;
        }
        set => player = value;
    }

    public override void Load()
    {
        _physCharacter = PhysicsManager.AddPlayerController(this);
        InputManager.RegisterInputHandler(this);

        base.Load();
    }

    public override void Unload()
    {
        PhysicsManager.RemovePlayerController();
        InputManager.UnregisterInputHandler(this);

        base.Unload();
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
        return Matrix4.CreatePerspectiveFieldOfView(_fov, AspectRatio, 1f, 10000f);
    }

    public Ray GetCameraToViewportRay(Vector2 screenPosition)
    {
        var inverseVp = (GetViewMatrix() * GetProjectionMatrix()).Inverted();

        var ndc = new Vector2(2.0f * screenPosition.X - 1.0f, 2.0f * (1.0f - screenPosition.Y) - 1f);
        var clip = new Vector4(ndc, -1.0f, 1.0f);

        var view = clip * inverseVp;
        view /= view.W;

        var nearPointWorld = new Vector3(view.X, view.Y, view.Z);
        var rayOrigin = GetPropertyValue<Vector3>("Position");
        var rayDirection = Vector3.Normalize(nearPointWorld - rayOrigin);

        return new Ray(rayOrigin, rayDirection);
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
        SetPropertyValue("Rotation", quatRotation);
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        _noclip = ConVarStorage.Get<bool>("edt_enable");

        return !_noclip ? PhysicsMove(keyboardState, mouseState, frameTime) : NoclipMove(keyboardState, mouseState, frameTime);
    }

    private bool PhysicsMove(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (MainWindow.Paused)
        {
            IsControllingCursor = false;
            return false;
        }

        var inputHandled = false;

        if (_physCharacter != null)
        {
            var cameraSpeed = keyboardState.IsKeyDown(Keys.LeftShift) ? camera_speed * 4 : camera_speed;

            SetPropertyValue("Position", _physCharacter.Position.ToOpentkVector());

            bool playerControlsHorizontalVelocity = _physCharacter.IsSupported;

            var verticalVelocity =
                (Vector3.Dot(_physCharacter.LinearVelocity.ToOpentkVector(), Vector3.UnitY) * Vector3.UnitY)
                .ToNumericsVector();

            var desiredVelocity = System.Numerics.Vector3.Zero;
            var newVelocity = verticalVelocity;

            if (_physCharacter.GroundState != GroundState.InAir)
            {
                newVelocity = _physCharacter.GroundVelocity;
            }

            if (keyboardState.IsKeyDown(Keys.W))
                desiredVelocity += new System.Numerics.Vector3(Front.X, 0, Front.Z) * cameraSpeed; // Forward 
            if (keyboardState.IsKeyDown(Keys.S))
                desiredVelocity -= new System.Numerics.Vector3(Front.X, 0, Front.Z) * cameraSpeed; // Backwards
            if (keyboardState.IsKeyDown(Keys.A))
                desiredVelocity -= new System.Numerics.Vector3(Right.X, 0, Right.Z) * cameraSpeed; // Left
            if (keyboardState.IsKeyDown(Keys.D))
                desiredVelocity += new System.Numerics.Vector3(Right.X, 0, Right.Z) * cameraSpeed; // Right

            // jump
            if (keyboardState.IsKeyDown(Keys.Space) && _physCharacter.GroundState != GroundState.InAir)
                desiredVelocity += System.Numerics.Vector3.UnitY * jump_velocity;

            Yaw += mouseState.Delta.X * sensitivity;
            Pitch -= mouseState.Delta.Y * sensitivity;

            inputHandled = desiredVelocity != System.Numerics.Vector3.Zero || mouseState.Delta != Vector2.Zero;

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

            IsControllingCursor = true;
        }

        return inputHandled;
    }

    private bool NoclipMove(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        var cameraSpeed = keyboardState.IsKeyDown(Keys.LeftShift) ? camera_speed * 4 : camera_speed;

        if (mouseState.IsButtonDown(MouseButton.Right))
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

            if (_physCharacter != null)
            {
                _physCharacter.Position = position.ToNumericsVector();
                _physCharacter.LinearVelocity = System.Numerics.Vector3.Zero;
            }

            Yaw += mouseState.Delta.X * sensitivity;
            Pitch -= mouseState.Delta.Y * sensitivity;

            if (!IsControllingCursor)
            {
                InputManager.CaptureInput(this);
                IsControllingCursor = true;
            }

            return true;
        }

        if (IsControllingCursor)
        {
            InputManager.ReleaseInput(this);
            IsControllingCursor = false;
        }

        return false;
    }
}