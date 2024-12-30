using System.Collections.Generic;
using System.Threading;
using Jellyfish.Audio;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Render;
using JoltPhysicsSharp;
using OpenTK.Mathematics;

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
    private JobSystem _jobSystem = null!;
    private bool _shouldStop;
    private const int update_rate = (int)(1.0 / 120.0 * 1000);

    private readonly Dictionary<BodyID, BaseEntity> _bodies = new();
    private CharacterVirtual? _character;

    private Sound? _impactSound;

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

    public static BodyID? AddStaticObject(MeshPart[] meshes, BaseEntity entity)
    {
        var initialPosition = entity.GetPropertyValue<Vector3>("Position");
        var initialRotation = entity.GetPropertyValue<Quaternion>("Rotation");

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
            initialRotation.ToNumericsQuaternion(),
            MotionType.Static,
            Layers.NonMoving);

        var bodyId = instance?._bodyInterface.CreateAndAddBody(bodySettings, Activation.DontActivate);
        if (bodyId == null)
            return null;

        instance?._bodies.Add(bodyId.Value, entity);

        return bodyId;
    }

    public static BodyID? AddDynamicObject(ShapeSettings shape, BaseEntity entity)
    {
        var initialPosition = entity.GetPropertyValue<Vector3>("Position");
        var initialRotation = entity.GetPropertyValue<Quaternion>("Rotation");

        var bodySettings = new BodyCreationSettings(shape,
            initialPosition.ToNumericsVector(),
            initialRotation.ToNumericsQuaternion(),
            MotionType.Dynamic,
            Layers.Moving);

        var bodyId =
            instance?._bodyInterface.CreateAndAddBody(bodySettings, Activation.Activate);

        if (bodyId == null)
            return null;

        instance?._bodies.Add(bodyId.Value, entity);

        return bodyId;
    }

    public static CharacterVirtual? AddPlayerController(BaseEntity entity)
    {
        if (instance == null)
            return null;

        var initialPosition = entity.GetPropertyValue<Vector3>("Position");

        var charSettings = new CharacterVirtualSettings
        {
            Shape = new CapsuleShape(48f, 10f),
            Mass = 50f,
            Up = System.Numerics.Vector3.UnitY
        };

        instance._character = new CharacterVirtual(charSettings, initialPosition.ToNumericsVector(), System.Numerics.Quaternion.Identity, 0, instance._physicsSystem);
        return instance._character;
    }

    public static void SetPosition(BodyID bodyId, Vector3 newPosition)
    {
        instance?._bodyInterface.SetPosition(bodyId, newPosition.ToNumericsVector(), Activation.Activate);
    }

    public static void SetRotation(BodyID bodyId, Quaternion newRotation)
    {
        instance?._bodyInterface.SetRotation(bodyId, newRotation.ToNumericsQuaternion(), Activation.Activate);
    }
    
    public static void RemoveObject(BodyID body)
    {
        instance?._bodyInterface.RemoveBody(body);
    }

    private void Run()
    {
        Log.Context(this).Information("Starting physics thread...");

        if (!Foundation.Init())
        {
            Log.Context(this).Error("Failed to start Jolt!");
        }

        Foundation.SetTraceHandler((message) =>
        {
            Log.Context(this).Information(message);
        });

        Foundation.SetAssertFailureHandler((inExpression, inMessage, inFile, inLine) =>
        {
            var message = inMessage ?? inExpression;
            Log.Context(this).Error($"[JoltPhysics] Assertion failure at {inFile}:{inLine}: {message}");
            return true;
        });

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
        
        _jobSystem = new JobSystemThreadPool();

        _physicsSystem = new PhysicsSystem(settings);
        _bodyInterface = _physicsSystem.BodyInterface;
        _physicsSystem.Gravity *= 100f;
        _physicsSystem.OptimizeBroadPhase();

        _impactSound = AudioManager.AddSound("sounds/impact.wav");
        _impactSound!.Volume = 0.5f;

        _physicsSystem.OnContactAdded += OnContactAdded;

        Log.Context(this).Information("Jolt ready!");
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
                    var position = _bodyInterface.GetPosition(bodyId).ToOpentkVector();
                    var rotation = _bodyInterface.GetRotation(bodyId).ToOpentkQuaternion();

                    entity.SetPropertyValue("Position", position);
                    entity.SetPropertyValue("Rotation", rotation);
                }
            }

            var error = _physicsSystem.Update(update_rate / 1000f, 2, _jobSystem);
            if (error != PhysicsUpdateError.None)
            {
                Log.Context(this).Warning("Physics simulation reported error {Error}!", error);
            }
        }

        _jobSystem.Dispose();
        _impactSound?.Dispose();
        _physicsSystem.Dispose();
        Foundation.Shutdown();
    }

    private void OnContactAdded(PhysicsSystem system, in Body body1, in Body body2, in ContactManifold manifold, in ContactSettings settings)
    {
        if (_impactSound != null)
        {
            _impactSound.Position = manifold.GetWorldSpaceContactPointOn1(1).ToOpentkVector();
            _impactSound.Play();
        }
    }

    public void Unload()
    {
        _shouldStop = true;
    }
}