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
    private Quaternion? _previousRotation;

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

    public override void Unload()
    {
        PhysicsManager.RemoveObject(_physicsBodyId);
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
        var midY = (maxX - minX) / 2f;
        var midZ = (maxX - minX) / 2f;

        var middleCoord = new System.Numerics.Vector3(midX, midY, midZ);

        var halfHeigth = midY;
        var horizontalRadius = Math.Max(maxX - minX, maxZ - minZ) / 2f;
        var radius = Math.Max(Math.Max(maxX - minX, maxZ - minZ), maxY - minY) / 2f;

        var rotated = horizontalRadius > halfHeigth;
        var rotateByX = midX > midZ;

        var type = GetPropertyValue<BoundingBoxType>("BoundingBox");
        ShapeSettings shape = type switch
        {
            BoundingBoxType.Sphere => new SphereShapeSettings(radius),
            BoundingBoxType.Capsule => rotated ? new CapsuleShapeSettings(horizontalRadius, halfHeigth) : new CapsuleShapeSettings(halfHeigth, horizontalRadius),
            BoundingBoxType.Box => new BoxShapeSettings(middleCoord),
            BoundingBoxType.Cylinder => new CylinderShapeSettings(halfHeigth, radius),
            _ => throw new ArgumentException("Unknown bounding box type"),
        };
        
        var rotation = GetPropertyValue<Quaternion>("Rotation");

        //float rotationX = rotated && rotateByX ? float.DegreesToRadians(rotation.X + 90) : float.DegreesToRadians(rotation.X);
        //float rotationZ = rotated && !rotateByX ? float.DegreesToRadians(rotation.Z + 90) : float.DegreesToRadians(rotation.Z);

        return new RotatedTranslatedShapeSettings(new System.Numerics.Vector3(0, halfHeigth, 0), rotation.ToNumericsQuaternion(), shape);
    }
}