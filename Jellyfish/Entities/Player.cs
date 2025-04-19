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
    private CharacterVirtual? _physCharacter;

    private const float camera_speed = 120.0f;
    private const float jump_velocity = 170.0f;
    private const float sensitivity = 0.2f;

    private const float height = 65;
    private const float width = 30;

    public override BoundingBox? BoundingBox { get; } =
        new BoundingBox(new Vector3(width / 2, height / 2, width / 2), new Vector3(-width / 2, -height / 2, -width / 2));

    public override void Think(float frameTime)
    {
        if (!ConVarStorage.Get<bool>("edt_enable"))
        {
            Camera.Instance.Position = GetPropertyValue<Vector3>("Position") + new Vector3(0, height / 2, 0);
            Camera.Instance.Rotation = GetPropertyValue<Quaternion>("Rotation");
        }
        else
        {
            if (_physCharacter != null)
            {
                _physCharacter.Position = GetPropertyValue<Vector3>("Position").ToNumericsVector();
                _physCharacter.Rotation = GetPropertyValue<Quaternion>("Rotation").ToNumericsQuaternion();
            }
        }

        SimulatePhysics(frameTime);

        base.Think(frameTime);
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
        var editorMode = ConVarStorage.Get<bool>("edt_enable");
        if (editorMode || MainWindow.Paused)
        {
            Camera.Instance.IsControllingCursor = false;
            return false;
        }

        Camera.Instance.Yaw += mouseState.Delta.X * sensitivity;
        Camera.Instance.Pitch -= mouseState.Delta.Y * sensitivity;
        Camera.Instance.IsControllingCursor = true;

        return PhysicsMove(keyboardState) || mouseState.Delta != Vector2.Zero;
    }

    private void SimulatePhysics(float frameTime)
    {
        if (_physCharacter == null) 
            return;

        SetPropertyValue("Position", _physCharacter.Position.ToOpentkVector());

        // apply gravity
        _physCharacter.LinearVelocity += System.Numerics.Vector3.UnitY * PhysicsManager.GetGravity() * frameTime;

        var groundVelocity = _physCharacter.LinearVelocity with { Y = 0 };

        // positive velocity mean we probably want to leave the ground but the physics engine didn't catch that yet
        var tryingToLiftOff = _physCharacter.LinearVelocity.Y > 0;
        if (_physCharacter.GroundState == GroundState.OnGround && !tryingToLiftOff)
            _physCharacter.LinearVelocity = groundVelocity;

        if (groundVelocity.Length() != 0)
        {
            var groundFriction = 12f;
            if (_physCharacter.GroundState != GroundState.OnGround)
                groundFriction = 1f;

            // actively try to stop the movement ground movement
            _physCharacter.LinearVelocity -= groundVelocity * groundFriction * frameTime;

            var groundDirection = System.Numerics.Vector3.Normalize(groundVelocity);

            var dot = System.Numerics.Vector3.Dot(_physCharacter.LinearVelocity, groundDirection);
            if (dot <= 0)
                _physCharacter.LinearVelocity = System.Numerics.Vector3.Zero with { Y = _physCharacter.LinearVelocity.Y };
        }
    }

    private bool PhysicsMove(KeyboardState keyboardState)
    {
        if (_physCharacter == null) 
            return false;

        var cameraSpeed = keyboardState.IsKeyDown(Keys.LeftShift) ? camera_speed * 2 : camera_speed;

        var direction = GetMovementDirection(keyboardState);

        var desiredVelocity = Vector3.Zero;

        // clamp desired ground velocity
        var directionVelocity = Vector3.Dot(_physCharacter.LinearVelocity.ToOpentkVector(), direction);
        if (directionVelocity < cameraSpeed)
        {
            var airStrafeMultiplier = 1f;
            if (_physCharacter.GroundState == GroundState.InAir)
                airStrafeMultiplier = 0.05f;

            desiredVelocity += direction * cameraSpeed * airStrafeMultiplier;
        }

        // jump
        if (keyboardState.IsKeyPressed(Keys.Space) && _physCharacter.IsSupported && _physCharacter.GroundState == GroundState.OnGround)
        {
            var verticalVelocity = Vector3.Dot(_physCharacter.LinearVelocity.ToOpentkVector(), Vector3.UnitY);
            if (verticalVelocity < jump_velocity)
                desiredVelocity += Vector3.UnitY * jump_velocity;
        }

        _physCharacter.LinearVelocity += desiredVelocity.ToNumericsVector();

        return desiredVelocity != Vector3.Zero;
    }

    public Frustum GetFrustum()
    {
        var cameraPosition = GetPropertyValue<Vector3>("Position") + new Vector3(0, height / 2, 0);
        var view = Matrix4.LookAt(cameraPosition, cameraPosition - Vector3.UnitZ, Vector3.UnitY);
        return new Frustum(view * Camera.Instance.GetProjectionMatrix());
    }

    private Vector3 GetMovementDirection(KeyboardState keyboardState)
    {
        var direction = Vector3.Zero;

        var forward = Camera.Instance.Front;
        var side = Camera.Instance.Right;

        // forward 
        if (keyboardState.IsKeyDown(Keys.W))
            direction += new Vector3(forward.X, 0, forward.Z);

        // backwards
        if (keyboardState.IsKeyDown(Keys.S))
            direction += -new Vector3(forward.X, 0, forward.Z);

        // left
        if (keyboardState.IsKeyDown(Keys.A))
            direction += -new Vector3(side.X, 0, side.Z);

        // right
        if (keyboardState.IsKeyDown(Keys.D))
            direction += new Vector3(side.X, 0, side.Z);

        return direction;
    }
}