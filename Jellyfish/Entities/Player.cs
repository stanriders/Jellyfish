using Jellyfish.Console;
using Jellyfish.Input;
using Jellyfish.Render;
using Jellyfish.Utils;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using BoundingBox = Jellyfish.Utils.BoundingBox;

namespace Jellyfish.Entities;

[Entity("player")]
public class Player : BaseEntity, IInputHandler, IHaveFrustum
{
    private bool _noclip;

    private CharacterVirtual? _physCharacter;

    private const float camera_speed = 120.0f;
    private const float jump_velocity = 170.0f;
    private const float sensitivity = 0.2f;

    private const float height = 65;
    private const float width = 30;

    public override BoundingBox? BoundingBox { get; } =
        new BoundingBox(new Vector3(width / 2, height / 2, width / 2), new Vector3(-width / 2, -height / 2, -width / 2));

    public override void Think()
    {
        Camera.Instance.Position = GetPropertyValue<Vector3>("Position") + new Vector3(0, height / 2, 0);
        Camera.Instance.Rotation = GetPropertyValue<Quaternion>("Rotation");

        base.Think();
    }

    public override void Load()
    {
        _physCharacter = PhysicsManager.AddPlayerController(this, new BoxShape((BoundingBox!.Value.Size / 2).ToNumericsVector()));
        InputManager.RegisterInputHandler(this);

        base.Load();
    }

    public override void Unload()
    {
        PhysicsManager.RemovePlayerController();
        InputManager.UnregisterInputHandler(this);

        base.Unload();
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        _noclip = ConVarStorage.Get<bool>("edt_enable");

        return !_noclip && PhysicsMove(keyboardState, mouseState, frameTime);
    }

    private bool PhysicsMove(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (MainWindow.Paused)
        {
            Camera.Instance.IsControllingCursor = false;
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
                desiredVelocity += new System.Numerics.Vector3(Camera.Instance.Front.X, 0, Camera.Instance.Front.Z) * cameraSpeed; // Forward 
            if (keyboardState.IsKeyDown(Keys.S))
                desiredVelocity -= new System.Numerics.Vector3(Camera.Instance.Front.X, 0, Camera.Instance.Front.Z) * cameraSpeed; // Backwards
            if (keyboardState.IsKeyDown(Keys.A))
                desiredVelocity -= new System.Numerics.Vector3(Camera.Instance.Right.X, 0, Camera.Instance.Right.Z) * cameraSpeed; // Left
            if (keyboardState.IsKeyDown(Keys.D))
                desiredVelocity += new System.Numerics.Vector3(Camera.Instance.Right.X, 0, Camera.Instance.Right.Z) * cameraSpeed; // Right

            // jump
            if (keyboardState.IsKeyDown(Keys.Space) && _physCharacter.GroundState != GroundState.InAir)
                desiredVelocity += System.Numerics.Vector3.UnitY * jump_velocity;

            Camera.Instance.Yaw += mouseState.Delta.X * sensitivity;
            Camera.Instance.Pitch -= mouseState.Delta.Y * sensitivity;

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

            Camera.Instance.IsControllingCursor = true;
        }

        return inputHandled;
    }

    public Frustum GetFrustum()
    {
        var cameraPosition = GetPropertyValue<Vector3>("Position") + new Vector3(0, height / 2, 0);
        var view = Matrix4.LookAt(cameraPosition, cameraPosition - Vector3.UnitZ, Vector3.UnitY);
        return new Frustum(view * Camera.Instance.GetProjectionMatrix());
    }
}