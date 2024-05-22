using System;
using System.Linq;
using JoltPhysicsSharp;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("model_dynamic")]
public class DynamicModel : BaseModelEntity
{
    private BodyID _physicsBodyId;
    private Vector3? _previousPosition;
    private Vector3? _previousRotation;

    public enum BoundingBoxType
    {
        Capsule,
        Box,
        Sphere
    }

    public DynamicModel()
    {
        AddProperty<string>("Model");
        AddProperty<BoundingBoxType>("BoundingBox");
    }

    public override void Load()
    {
        ModelPath = $"models/{GetPropertyValue<string>("Model")}";
        base.Load();

        _physicsBodyId = PhysicsManager.AddDynamicObject(CalculatePhysicsShape(), this);
    }

    public override void Think()
    {
        if (Model != null)
        {
            Model.Position = GetPropertyValue<Vector3>("Position");
            Model.Rotation = GetPropertyValue<Vector3>("Rotation");

            if (_previousPosition != Model.Position)
            {
                PhysicsManager.SetPosition(_physicsBodyId, Model.Position);
                _previousPosition = Model.Position;
            }

            if (_previousRotation != Model.Rotation)
            {
                PhysicsManager.SetRotation(_physicsBodyId, Model.Rotation);
                _previousRotation = Model.Rotation;
            }
        }

        base.Think();
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

        var halfHeigth = (maxY - minY) / 2;
        var horizontalRadius = Math.Max(maxX - minX, maxZ - minZ) / 2f;
        var radius = Math.Max(Math.Max(maxX - minX, maxZ - minZ), maxY - minY) / 2f;

        var type = GetPropertyValue<BoundingBoxType>("BoundingBox");
        switch (type)
        {
            case BoundingBoxType.Sphere:
                return new SphereShapeSettings(radius);
            case BoundingBoxType.Capsule:
                return new CapsuleShapeSettings(halfHeigth, horizontalRadius);
            case BoundingBoxType.Box:
                return new BoxShapeSettings(new System.Numerics.Vector3((maxX - minX) / 2f, halfHeigth, (maxZ - minZ) / 2f));
            default:
                throw new ArgumentException("Unknown bounding box type");
        }
    }
}