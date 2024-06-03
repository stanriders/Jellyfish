using System.Collections.Generic;
using System.Threading;
using Jellyfish.Entities;
using Jellyfish.Render;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish;

public class PhysicsManager
{
    public bool ShouldSimulate { get; set; }
    public bool IsReady { get; private set; }

    private static class Layers
    {
        public static readonly ObjectLayer NonMoving = 0;
        public static readonly ObjectLayer Moving = 1;
    }

    private static class BroadPhaseLayers
    {
        public static readonly BroadPhaseLayer NonMoving = 0;
        public static readonly BroadPhaseLayer Moving = 1;
    }

    private System.Numerics.Vector3 Gravity => _physicsSystem.Gravity;

    private PhysicsSystem _physicsSystem = null!;
    private BodyInterface _bodyInterface;
    private bool _shouldStop;
    private const int update_rate = (int)(1.0 / 120.0 * 1000);

    private readonly Dictionary<BodyID, BaseEntity> _bodies = new();
    private CharacterVirtual? _character;

    private static PhysicsManager? instance;

    public PhysicsManager()
    {
        var physicsThread = new Thread(Run) { Name = "Physics thread" };
        physicsThread.Start();

        instance = this;
    }

    public static System.Numerics.Vector3 GetGravity()
    {
        return instance?.Gravity ?? System.Numerics.Vector3.Zero;
    }

    public static void AddStaticObject(MeshPart[] meshes, BaseEntity entity)
    {
        instance?.AddStaticObjectInternal(meshes, entity);
    }

    public static BodyID AddDynamicObject(ShapeSettings shape, BaseEntity entity)
    {
        return instance?.AddDynamicObjectInternal(shape, entity) ?? default;
    }

    public static CharacterVirtual AddPlayerController(BaseEntity entity)
    {
        return instance?.AddPlayerControllerInternal(entity)!;
    }

    private CharacterVirtual AddPlayerControllerInternal(BaseEntity entity)
    {
        var initialPosition = entity.GetPropertyValue<Vector3>("Position");

        var charSettings = new CharacterVirtualSettings
        {
            Shape = new CapsuleShape(48f, 10f),
            Mass = 50f,
            Up = System.Numerics.Vector3.UnitY
        };

        _character = new CharacterVirtual(charSettings, initialPosition.ToNumericsVector(), System.Numerics.Quaternion.Identity, 0, _physicsSystem);
        return _character;
    }

    public static void SetPosition(BodyID bodyId, Vector3 newPosition)
    {
        instance?.SetPositionInternal(bodyId, newPosition);
    }

    private void SetPositionInternal(BodyID bodyId, Vector3 newPosition)
    {
        _bodyInterface.SetPosition(bodyId, newPosition.ToNumericsVector(), Activation.Activate);
    }

    public static void SetRotation(BodyID bodyId, Vector3 newRotation)
    {
        instance?.SetRotationInternal(bodyId, newRotation);
    }

    private void SetRotationInternal(BodyID bodyId, Vector3 newRotation)
    {
        var quatRotation = Quaternion.FromEulerAngles(float.DegreesToRadians(newRotation.X),
            float.DegreesToRadians(newRotation.Y), float.DegreesToRadians(newRotation.Z));

        _bodyInterface.SetRotation(bodyId, quatRotation.ToNumericsQuaternion(), Activation.Activate);
    }

    private void AddStaticObjectInternal(MeshPart[] meshes, BaseEntity entity)
    {
        var initialPosition = entity.GetPropertyValue<Vector3>("Position");
        var initialRotation = entity.GetPropertyValue<Vector3>("Rotation");
        var quatRotation = Quaternion.FromEulerAngles(float.DegreesToRadians(initialRotation.X),
            float.DegreesToRadians(initialRotation.Y), float.DegreesToRadians(initialRotation.Z));

        var triangles = new List<Triangle>();

        foreach (var mesh in meshes)
        {
            if (mesh.Indices is { Count: > 0 })
            {
                for (var i = 0; i < mesh.Indices.Count; i += 3)
                {
                    var v1 = mesh.Vertices[(int)mesh.Indices[i]].Coordinates.ToNumericsVector();
                    var v2 = mesh.Vertices[(int)mesh.Indices[i + 1]].Coordinates.ToNumericsVector();
                    var v3 = mesh.Vertices[(int)mesh.Indices[i + 2]].Coordinates.ToNumericsVector();

                    triangles.Add(new Triangle(v1, v2, v3));
                }
            }
            else
            {
                for (var i = 0; i < mesh.Vertices.Count; i += 3)
                {
                    var v1 = mesh.Vertices[i].Coordinates.ToNumericsVector();
                    var v2 = mesh.Vertices[i + 1].Coordinates.ToNumericsVector();
                    var v3 = mesh.Vertices[i + 2].Coordinates.ToNumericsVector();

                    triangles.Add(new Triangle(v1, v2, v3));
                }
            }
        }

        var shapeSettings = new MeshShapeSettings(triangles.ToArray());

        var bodySettings = new BodyCreationSettings(shapeSettings,
            initialPosition.ToNumericsVector(),
            quatRotation.ToNumericsQuaternion(),
            MotionType.Static,
            Layers.NonMoving);

        var bodyId = _bodyInterface.CreateAndAddBody(bodySettings, Activation.DontActivate);

        _bodies.Add(bodyId, entity);
    }

    private BodyID AddDynamicObjectInternal(ShapeSettings shape, BaseEntity entity)
    {
        var initialPosition = entity.GetPropertyValue<Vector3>("Position");
        var initialRotation = entity.GetPropertyValue<Vector3>("Rotation");
        var quatRotation = Quaternion.FromEulerAngles(float.DegreesToRadians(initialRotation.X),
            float.DegreesToRadians(initialRotation.Y), float.DegreesToRadians(initialRotation.Z));

        var bodySettings = new BodyCreationSettings(shape,
            initialPosition.ToNumericsVector(),
            quatRotation.ToNumericsQuaternion(),
            MotionType.Dynamic,
            Layers.Moving);

        var bodyId =
            _bodyInterface.CreateAndAddBody(bodySettings, Activation.Activate);

        _bodies.Add(bodyId, entity);

        return bodyId;
    }

    private void Run()
    {
        Log.Information("[PhysicsManager] Starting physics thread...");

        if (!Foundation.Init())
        {
            Log.Information("[PhysicsManager] Failed to start Jolt!");
        }

        // We use only 2 layers: one for non-moving objects and one for moving objects
        ObjectLayerPairFilterTable objectLayerPairFilterTable = new(2);
        objectLayerPairFilterTable.EnableCollision(Layers.NonMoving, Layers.Moving);
        objectLayerPairFilterTable.EnableCollision(Layers.Moving, Layers.Moving);

        // We use a 1-to-1 mapping between object layers and broadphase layers
        BroadPhaseLayerInterfaceTable broadPhaseLayerInterfaceTable = new(2, 2);
        broadPhaseLayerInterfaceTable.MapObjectToBroadPhaseLayer(Layers.NonMoving, BroadPhaseLayers.NonMoving);
        broadPhaseLayerInterfaceTable.MapObjectToBroadPhaseLayer(Layers.Moving, BroadPhaseLayers.Moving);

        ObjectLayerPairFilter objectLayerPairFilter = objectLayerPairFilterTable;
        BroadPhaseLayerInterface broadPhaseLayerInterface = broadPhaseLayerInterfaceTable;
        ObjectVsBroadPhaseLayerFilter objectVsBroadPhaseLayerFilter = new ObjectVsBroadPhaseLayerFilterTable(broadPhaseLayerInterfaceTable, 2, objectLayerPairFilterTable, 2);

        var settings = new PhysicsSystemSettings
        {
            ObjectLayerPairFilter = objectLayerPairFilter,
            BroadPhaseLayerInterface = broadPhaseLayerInterface,
            ObjectVsBroadPhaseLayerFilter = objectVsBroadPhaseLayerFilter
        };

        _physicsSystem = new PhysicsSystem(settings);
        _bodyInterface = _physicsSystem.BodyInterface;
        _physicsSystem.Gravity *= 100f;
        _physicsSystem.OptimizeBroadPhase();

        Log.Information("[PhysicsManager] Jolt ready!");
        IsReady = true;

        while (!_shouldStop)
        {
            Thread.Sleep(update_rate);

            if (!ShouldSimulate)
                continue;

            _character?.Update(update_rate / 1000f, Layers.Moving, _physicsSystem);

            foreach (var (bodyId, entity) in _bodies)
            {
                if (_bodyInterface.IsActive(bodyId))
                {
                    var centerOfMassPosition = _bodyInterface.GetPosition(bodyId).ToOpentkVector();
                    var rotation = _bodyInterface.GetRotation(bodyId).ToOpentkQuaternion().ToEulerAngles();
                    var angleRotation = new Vector3(float.RadiansToDegrees(rotation.X), float.RadiansToDegrees(rotation.Y), float.RadiansToDegrees(rotation.Z));

                    entity.SetPropertyValue("Position", centerOfMassPosition);
                    entity.SetPropertyValue("Rotation", angleRotation);
                }
            }

            var error = _physicsSystem.Step(update_rate / 1000f, 2);
            if (error != 0)
            {
                Log.Warning("[PhysicsManager] Physics simulation reported error {Error}!", error);
            }
        }

        _physicsSystem.Dispose();
        Foundation.Shutdown();
    }

    public void Unload()
    {
        _shouldStop = true;
    }
}