using System;
using System.Collections.Generic;
using System.Linq;
using JoltPhysicsSharp;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("model_dynamic")]
public class DynamicModel : BaseModelEntity
{
    private BodyID? _physicsBodyId;

    // TODO: add property change callbacks
    private Vector3? _previousPosition;
    private Quaternion? _previousRotation;
    private bool _previousEnablePhysics = true; 

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
        AddProperty<bool>("EnablePhysics", true);
    }

    public override void Load()
    {
        ModelPath = $"models/{GetPropertyValue<string>("Model")}";
        base.Load();

        _physicsBodyId = PhysicsManager.AddDynamicObject(CalculatePhysicsShape(), this) ?? 0;
    }

    public override void Think()
    {
        if (Model != null)
        {
            Model.Position = GetPropertyValue<Vector3>("Position");
            Model.Rotation = GetPropertyValue<Quaternion>("Rotation");

            if (_physicsBodyId != null)
            {
                if (_previousPosition != Model.Position)
                {
                    PhysicsManager.SetPosition(_physicsBodyId.Value, Model.Position);
                    _previousPosition = Model.Position;
                }

                if (_previousRotation != Model.Rotation)
                {
                    PhysicsManager.SetRotation(_physicsBodyId.Value, Model.Rotation);
                    _previousRotation = Model.Rotation;
                }
            }

            var enablePhysics = GetPropertyValue<bool>("EnablePhysics");
            if (_previousEnablePhysics != enablePhysics)
            {
                if (enablePhysics)
                    _physicsBodyId = PhysicsManager.AddDynamicObject(CalculatePhysicsShape(), this) ?? 0;
                else
                    PhysicsManager.RemoveObject(_physicsBodyId!.Value);

                _previousEnablePhysics = enablePhysics;
            }
        }

        base.Think();
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

        var midX = (maxX - minX) / 2f;
        var midY = (maxY - minY) / 2f;
        var midZ = (maxZ - minZ) / 2f;

        var middleCoord = new System.Numerics.Vector3(midX, midY, midZ);

        var halfHeigth = midY;
        var horizontalRadius = Math.Max(maxX - minX, maxZ - minZ) / 2f;
        var radius = Math.Max(Math.Max(maxX - minX, maxZ - minZ), maxY - minY) / 2f;

        var type = GetPropertyValue<BoundingBoxType>("BoundingBox");

        ShapeSettings shape = type switch
        {
            BoundingBoxType.Sphere => new SphereShapeSettings(radius),
            BoundingBoxType.Capsule => new CapsuleShapeSettings(radius, horizontalRadius),
            BoundingBoxType.Box => new BoxShapeSettings(middleCoord),
            BoundingBoxType.Cylinder => new CylinderShapeSettings(horizontalRadius, halfHeigth),
            _ => throw new ArgumentException("Unknown bounding box type"),
        };

        var rotation = GetPropertyValue<Quaternion>("Rotation");

        return new RotatedTranslatedShapeSettings(new System.Numerics.Vector3(0, 0, midZ), rotation.ToNumericsQuaternion(), shape);
    }
}