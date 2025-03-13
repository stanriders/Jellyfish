using System;
using System.Linq;
using JoltPhysicsSharp;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("model_dynamic")]
public class DynamicModel : BaseModelEntity
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

        if (GetPropertyValue<bool>("EnablePhysics"))
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

    public override void Unload()
    {
        if (_physicsBodyId != null)
            PhysicsManager.RemoveObject(_physicsBodyId.Value);

        base.Unload();
    }

    public virtual ShapeSettings CalculatePhysicsShape()
    {
        var maxY = 0f;
        var minY = 0f;
        var maxX = 0f;
        var minX = 0f;
        var maxZ = 0f;
        var minZ = 0f;

        foreach (var vertex in Model!.Meshes.Select(x => x.MeshPart).SelectMany(x => x.Vertices))
        {
            var coords = vertex.Coordinates;
            if (coords.X < minX)
                minX = coords.X;

            if (coords.X > maxX)
                maxX = coords.X;

            if (coords.Z < minZ)
                minZ = coords.Z;

            if (coords.Z > maxZ)
                maxZ = coords.Z;

            if (coords.Y < minY)
                minY = coords.Y;

            if (coords.Y > maxY)
                maxY = coords.Y;
        }

        var midX = (maxX + minX) / 2f;
        var midY = (maxY + minY) / 2f;
        var midZ = (maxZ + minZ) / 2f;

        var middleCoord = new System.Numerics.Vector3(midX, midY, midZ);

        var halfHeigth = maxY - midY;
        var horizontalRadius = Math.Max(maxX + minX, maxZ + minZ) / 2f;
        var radius = Math.Max(Math.Max(maxX + minX, maxZ + minZ), maxY + minY) / 2f;

        var type = GetPropertyValue<BoundingBoxType>("BoundingBox");

        ShapeSettings shape = type switch
        {
            BoundingBoxType.Sphere => new SphereShapeSettings(radius),
            BoundingBoxType.Capsule => new CapsuleShapeSettings(radius, horizontalRadius),
            BoundingBoxType.Box => new BoxShapeSettings(new System.Numerics.Vector3(maxX, maxY, maxZ)),
            BoundingBoxType.Cylinder => new CylinderShapeSettings(horizontalRadius, halfHeigth),
            _ => throw new ArgumentException("Unknown bounding box type"),
        };

        var rotation = GetPropertyValue<Quaternion>("Rotation");

        return new RotatedTranslatedShapeSettings(middleCoord, rotation.ToNumericsQuaternion(), shape);
    }
}