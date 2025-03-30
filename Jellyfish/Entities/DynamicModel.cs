using System;
using System.Linq;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using BoundingBox = Jellyfish.Utils.BoundingBox;

namespace Jellyfish.Entities;

[Entity("model_dynamic")]
public class DynamicModel : BaseModelEntity, IPhysicsEntity
{
    private BodyID? _physicsBodyId;

    public enum BoundingBoxType
    {
        Capsule,
        Box,
        Sphere,
        Cylinder
    }

    public override bool DrawDevCone => true;

    public DynamicModel()
    {
        AddProperty<string>("Model", editable: false);
        AddProperty<BoundingBoxType>("BoundingBox", editable: false);
        AddProperty("EnablePhysics", true, changeCallback: enablePhysics =>
        {
            if (enablePhysics)
            {
                _physicsBodyId = PhysicsManager.AddDynamicObject(CalculatePhysicsShape(), this) ?? 0;
            }
            else if (_physicsBodyId != null)
            {
                PhysicsManager.RemoveObject(_physicsBodyId!.Value);
                _physicsBodyId = null;
            }
        });
    }

    public override void Load()
    {
        ModelPath = $"models/{GetPropertyValue<string>("Model")}";
        base.Load();

        if (GetPropertyValue<bool>("EnablePhysics") && Model != null)
            _physicsBodyId = PhysicsManager.AddDynamicObject(CalculatePhysicsShape(), this) ?? 0;
    }

    protected override void OnPositionChanged(Vector3 position)
    {
        if (_physicsBodyId != null)
        {
            PhysicsManager.SetPosition(_physicsBodyId.Value, position);
        }

        base.OnPositionChanged(position);
    }

    protected override void OnRotationChanged(Quaternion rotation)
    {
        if (_physicsBodyId != null)
        {
            PhysicsManager.SetRotation(_physicsBodyId.Value, rotation);
        }

        base.OnRotationChanged(rotation);
    }

    protected override void OnScaleChanged(Vector3 scale)
    {
        if (GetPropertyValue<bool>("EnablePhysics"))
        {
            // disable physics after scaling to make model not spazz out
            SetPropertyValue("EnablePhysics", false);
        }

        base.OnScaleChanged(scale);
    }

    public override void Unload()
    {
        if (_physicsBodyId != null)
            PhysicsManager.RemoveObject(_physicsBodyId.Value);

        base.Unload();
    }

    public virtual ShapeSettings CalculatePhysicsShape()
    {
        var boundingBox = new BoundingBox(Model!.Meshes.Select(x => x.MeshPart).Select(x => x.BoundingBox).ToArray());

        var halfHeigth = boundingBox.Size.Y / 2f;
        var horizontalRadius = Math.Max(boundingBox.Size.X, boundingBox.Size.Z) / 2f;
        var radius = Math.Max(Math.Max(boundingBox.Size.X, boundingBox.Size.Z), boundingBox.Size.Y) / 2f;

        var type = GetPropertyValue<BoundingBoxType>("BoundingBox");

        ShapeSettings shape = type switch
        {
            BoundingBoxType.Sphere => new SphereShapeSettings(radius),
            BoundingBoxType.Capsule => new CapsuleShapeSettings(halfHeigth, horizontalRadius),
            BoundingBoxType.Box => new BoxShapeSettings(new System.Numerics.Vector3(boundingBox.Size.X / 2f, boundingBox.Size.Y / 2f, boundingBox.Size.Z / 2f)),
            BoundingBoxType.Cylinder => new CylinderShapeSettings(halfHeigth, horizontalRadius),
            _ => throw new ArgumentException("Unknown bounding box type"),
        };

        var rotation = GetPropertyValue<Quaternion>("Rotation");
        var scale = GetPropertyValue<Vector3>("Scale");

        return new RotatedTranslatedShapeSettings(boundingBox.Center.ToNumericsVector(), System.Numerics.Quaternion.Identity, new ScaledShapeSettings(shape, scale.ToNumericsVector()));
    }

    public void ResetVelocity()
    {
        if (_physicsBodyId != null)
            PhysicsManager.SetVelocity(_physicsBodyId.Value, Vector3.Zero);
    }

    public void OnPhysicsPositionChanged(Vector3 position)
    {
        SetPropertyValue("Position", position);
    }

    public void OnPhysicsRotationChanged(Quaternion rotation)
    {
        SetPropertyValue("Rotation", rotation);
    }
}