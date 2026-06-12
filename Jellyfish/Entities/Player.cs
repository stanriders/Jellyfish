using Jellyfish.Console;
using Jellyfish.Input;
using Jellyfish.Utils;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using BoundingBox = Jellyfish.Utils.BoundingBox;

namespace Jellyfish.Entities;

[Entity("player")]
public class Player : BaseEntity, IInputHandler, IHaveFrustum
{
    public override bool DrawDevCone { get; set; } = true;

    private CharacterVirtual? _physCharacter;
    private bool _crouching;
    private bool _sprinting;
    private bool _jumping;

    private const float walk_velocity = 120.0f;
    private const float jump_velocity = 170.0f;
    private const float sensitivity = 0.2f;

    private const float height = 65;
    private const float width = 30;

    private Vector3 _desiredDirection = Vector3.Zero;

    public override BoundingBox? BoundingBox { get; } = new(new Vector3(width / 2, height / 2, width / 2), new Vector3(-width / 2, -height / 2, -width / 2));

    public override void Think(float frameTime)
    {
        if (!ConVarStorage.Get<bool>("edt_enable"))
        {
            Engine.MainViewport.Position = GetPropertyValue<Vector3>("Position") + new Vector3(0, height / (_crouching ? 4 : 2), 0);
            Engine.MainViewport.Rotation = GetPropertyValue<Quaternion>("Rotation");
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
        _physCharacter = Engine.PhysicsManager.AddPlayerController(this, new BoxShape((BoundingBox!.Value.Size / 2).ToNumericsVector()));
        Engine.InputManager.RegisterInputHandler(this);

        base.Load();
    }

    public override void Unload()
    {
        Engine.PhysicsManager.RemovePlayerController();
        Engine.InputManager.UnregisterInputHandler(this);

        base.Unload();
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        var editorMode = ConVarStorage.Get<bool>("edt_enable");
        if (editorMode || Engine.Paused)
        {
            Engine.InputManager.IsControllingCursor = false;
            return false;
        }

        Engine.MainViewport.Yaw += mouseState.Delta.X * sensitivity;
        Engine.MainViewport.Pitch -= mouseState.Delta.Y * sensitivity;
        Engine.InputManager.IsControllingCursor = true;

        if (_physCharacter == null)
            return false;

        var direction = Vector3.Zero;

        var forward = Engine.MainViewport.Front;
        var side = Engine.MainViewport.Right;

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

        _crouching = keyboardState.IsKeyDown(Keys.LeftControl);
        _sprinting = keyboardState.IsKeyDown(Keys.LeftShift);
        _jumping = keyboardState.IsKeyDown(Keys.Space);

        _desiredDirection = direction;

        return _desiredDirection != Vector3.Zero || mouseState.Delta != Vector2.Zero;
    }

    private void SimulatePhysics(float frameTime)
    {
        if (_physCharacter == null) 
            return;

        SetPropertyValue("Position", (Vector3)_physCharacter.Position);

        // apply gravity
        _physCharacter.LinearVelocity += System.Numerics.Vector3.UnitY * Engine.PhysicsManager.Gravity * frameTime;

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

            // actively try to stop the ground movement
            _physCharacter.LinearVelocity -= groundVelocity * groundFriction * frameTime;

            var groundDirection = System.Numerics.Vector3.Normalize(groundVelocity);

            var dot = System.Numerics.Vector3.Dot(_physCharacter.LinearVelocity, groundDirection);
            if (dot <= 0)
                _physCharacter.LinearVelocity = System.Numerics.Vector3.Zero with { Y = _physCharacter.LinearVelocity.Y };
        }

        var velocity = walk_velocity;
        if (_sprinting)
            velocity *= 2;
        else if (_crouching)
            velocity /= 2;

        var direction = _desiredDirection;

        var desiredVelocity = Vector3.Zero;

        // clamp desired ground velocity
        var directionVelocity = Vector3.Dot((Vector3)_physCharacter.LinearVelocity, direction);
        if (directionVelocity < velocity)
        {
            var airStrafeMultiplier = 1f;
            if (_physCharacter.GroundState == GroundState.InAir)
                airStrafeMultiplier = 0.05f;

            desiredVelocity += direction * velocity * airStrafeMultiplier;
        }

        if (_jumping && _physCharacter!.IsSupported && _physCharacter!.GroundState == GroundState.OnGround)
        {
            var verticalVelocity = Vector3.Dot((Vector3)_physCharacter.LinearVelocity, Vector3.UnitY);
            if (verticalVelocity < jump_velocity)
                desiredVelocity += Vector3.UnitY * jump_velocity;
        }

        _physCharacter.LinearVelocity += desiredVelocity.ToNumericsVector();
    }

    public Frustum GetFrustum()
    {
        var cameraPosition = GetPropertyValue<Vector3>("Position") + new Vector3(0, height / (_crouching ? 4 : 2), 0);
        var view = Matrix4.LookAt(cameraPosition, cameraPosition - Vector3.UnitZ, Vector3.UnitY);
        return new Frustum(view * Engine.MainViewport.GetProjectionMatrix());
    }
}