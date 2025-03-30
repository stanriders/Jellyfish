using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ImGuiNET;
using Jellyfish.Audio;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Render;
using JoltPhysicsSharp;
using OpenTK.Mathematics;
using Mesh = Jellyfish.Render.Mesh;
using Quaternion = OpenTK.Mathematics.Quaternion;

namespace Jellyfish;

public class EnableDebugRenderer() : ConVar<bool>("phys_debug", false);
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
    private const int update_rate = (int)(1.0 / 240.0 * 1000);

    private readonly Dictionary<BodyID, IPhysicsEntity> _bodies = new();
    private CharacterVirtual? _character;

    private readonly Queue<BodyID> _deletionQueue = new();

    private Sound? _impactSound;

    private PhysicsDebugRenderer? _debugRenderer;
    private readonly Mesh _debugMesh;
    private readonly PhysicsDebugDrawFilter _debugDrawFilter = new();

    private static PhysicsManager? instance;

    public PhysicsManager()
    {
        _debugMesh = new Mesh(new MeshPart { Name = "physdebug" }) { IsDev = true };
        MeshManager.AddMesh(_debugMesh);

        var physicsThread = new Thread(Run) { Name = "Physics thread" };
        physicsThread.Start();

        instance = this;
    }

    public static System.Numerics.Vector3 GetGravity()
    {
        return instance?.Gravity ?? System.Numerics.Vector3.Zero;
    }

    public static BodyID? AddStaticObject(MeshPart[] meshes, IPhysicsEntity entity)
    {
        if (entity is not BaseEntity baseEntity)
        {
            Log.Context(nameof(PhysicsManager)).Error("Physics entity {entity} isn't inheriting BaseEntity!", entity);
            return null;
        }

        var initialPosition = baseEntity.GetPropertyValue<Vector3>("Position");
        var initialRotation = baseEntity.GetPropertyValue<Quaternion>("Rotation");
        var initialScale = baseEntity is BaseModelEntity ? baseEntity.GetPropertyValue<Vector3>("Scale") : Vector3.One;

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

        using var shapeSettings = new ScaledShape(new MeshShape(new MeshShapeSettings(triangles.ToArray())),initialScale.ToNumericsVector());

        using var bodySettings = new BodyCreationSettings(shapeSettings,
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

    public static BodyID? AddDynamicObject(ShapeSettings shape, IPhysicsEntity entity)
    {
        if (entity is not BaseEntity baseEntity)
        {
            Log.Context(nameof(PhysicsManager)).Error("Physics entity {entity} isn't inheriting BaseEntity!", entity);
            return null;
        }

        var initialPosition = baseEntity.GetPropertyValue<Vector3>("Position");
        var initialRotation = baseEntity.GetPropertyValue<Quaternion>("Rotation");

        using var bodySettings = new BodyCreationSettings(shape,
            initialPosition.ToNumericsVector(),
            initialRotation.ToNumericsQuaternion(),
            MotionType.Dynamic,
            Layers.Moving);

        bodySettings.OverrideMassProperties = OverrideMassProperties.CalculateInertia;
        bodySettings.MassPropertiesOverride = new MassProperties { Mass = 1f };

        var bodyId =
            instance?._bodyInterface.CreateAndAddBody(bodySettings, Activation.Activate);

        if (bodyId == null)
            return null;

        instance?._bodies.Add(bodyId.Value, entity);

        shape.Dispose();

        instance?._physicsSystem.OptimizeBroadPhase();

        return bodyId;
    }

    public static CharacterVirtual? AddPlayerController(BaseEntity entity)
    {
        if (instance == null)
            return null;

        var initialPosition = entity.GetPropertyValue<Vector3>("Position");

        var charSettings = new CharacterVirtualSettings
        {
            Shape = new BoxShape(new System.Numerics.Vector3(15f, 65f, 15f)),
            Mass = 100f,
            Up = System.Numerics.Vector3.UnitY,
            MaxSlopeAngle = 60
        };

        instance._character = new CharacterVirtual(charSettings, initialPosition.ToNumericsVector(), System.Numerics.Quaternion.Identity, 0, instance._physicsSystem);
        return instance._character;
    }

    public static void RemovePlayerController()
    {
        if (instance?._character == null)
            return;

        instance._character?.Dispose();
        instance._character = null;
    }

    public static void SetPosition(BodyID bodyId, Vector3 newPosition)
    {
        instance?._bodyInterface.SetPosition(bodyId, newPosition.ToNumericsVector(), Activation.Activate);
    }

    public static void SetRotation(BodyID bodyId, Quaternion newRotation)
    {
        instance?._bodyInterface.SetRotation(bodyId, newRotation.ToNumericsQuaternion(), Activation.Activate);
    }

    public static void SetVelocity(BodyID bodyId, Vector3 newVelocity)
    {
        instance?._bodyInterface.SetLinearVelocity(bodyId, newVelocity.ToNumericsVector());
    }

    public static void RemoveObject(BodyID body)
    {
        instance?._deletionQueue.Enqueue(body);
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

        _jobSystem = new JobSystemThreadPool();
        _debugRenderer = new PhysicsDebugRenderer(_debugMesh);

        // We use only 2 layers: one for non-moving objects and one for moving objects
        ObjectLayerPairFilterTable objectLayerPairFilter = new(2);
        objectLayerPairFilter.EnableCollision(Layers.NonMoving, Layers.Moving);
        objectLayerPairFilter.EnableCollision(Layers.Moving, Layers.Moving);

        // We use a 1-to-1 mapping between object layers and broadphase layers
        BroadPhaseLayerInterfaceTable broadPhaseLayerInterface = new(2, 2);
        broadPhaseLayerInterface.MapObjectToBroadPhaseLayer(Layers.NonMoving, BroadPhaseLayers.NonMoving);
        broadPhaseLayerInterface.MapObjectToBroadPhaseLayer(Layers.Moving, BroadPhaseLayers.Moving);

        ObjectVsBroadPhaseLayerFilterTable objectVsBroadPhaseLayerFilter = new(broadPhaseLayerInterface, 2, objectLayerPairFilter, 2);

        var settings = new PhysicsSystemSettings
        {
            MaxBodies = 65536,
            MaxBodyPairs = 65536,
            MaxContactConstraints = 65536,
            NumBodyMutexes = 0,
            ObjectLayerPairFilter = objectLayerPairFilter,
            BroadPhaseLayerInterface = broadPhaseLayerInterface,
            ObjectVsBroadPhaseLayerFilter = objectVsBroadPhaseLayerFilter
        };

        _physicsSystem = new PhysicsSystem(settings);
        _bodyInterface = _physicsSystem.BodyInterface;
        _physicsSystem.Gravity *= 80f;
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

            while (_deletionQueue.TryDequeue(out var bodyId))
            {
                _bodyInterface.DeactivateBody(bodyId);
                _bodyInterface.RemoveAndDestroyBody(bodyId);
                _bodies.Remove(bodyId);
            }

            foreach (var (bodyId, entity) in _bodies)
            {
                if (_bodyInterface.IsActive(bodyId))
                {
                    var position = _bodyInterface.GetPosition(bodyId).ToOpentkVector();
                    var rotation = _bodyInterface.GetRotation(bodyId).ToOpentkQuaternion();

                    entity.OnPhysicsPositionChanged(position);
                    entity.OnPhysicsRotationChanged(rotation);
                }
            }

            _character?.ExtendedUpdate(update_rate / 1000f, new ExtendedUpdateSettings(), Layers.Moving, _physicsSystem);
            var error = _physicsSystem.Update(update_rate / 1000f, 1, _jobSystem);
            if (error != PhysicsUpdateError.None)
            {
                Log.Context(this).Warning("Physics simulation reported error {Error}!", error);
            }

            var drawSettings = new DrawSettings
            {
                DrawMassAndInertia = true,
                DrawVelocity = true,
                DrawShape = true,
                DrawShapeColor = ShapeColor.MotionTypeColor
            };
            _physicsSystem.DrawBodies(drawSettings, _debugRenderer, _debugDrawFilter);
            _debugRenderer.Render();
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

    public class PhysicsDebugRenderer : DebugRenderer
    {
        private readonly List<Vertex> _vertices = new();

        private readonly Mesh _mesh;

        public PhysicsDebugRenderer(Mesh mesh)
        {
            _mesh = mesh;
        }

        protected override void DrawLine(System.Numerics.Vector3 from, System.Numerics.Vector3 to, JoltColor color)
        {
            Debug.DrawLine(from.ToOpentkVector(), to.ToOpentkVector());
        }

        protected override void DrawText3D(System.Numerics.Vector3 position, string? text, JoltColor color, float height = 0.5f)
        {
            Debug.DrawText(position.ToOpentkVector(), text ?? "");
        }
        
        protected override void DrawTriangle(System.Numerics.Vector3 v1, System.Numerics.Vector3 v2, System.Numerics.Vector3 v3, JoltColor color, CastShadow castShadow = CastShadow.Off)
        {
            _vertices.AddRange([
                new Vertex { Coordinates = v1.ToOpentkVector(), Normal = new Vector3(1), UV = new Vector2(0,1) },
                new Vertex { Coordinates = v2.ToOpentkVector(), Normal = new Vector3(1), UV = new Vector2(0,1) },
                new Vertex { Coordinates = v3.ToOpentkVector(), Normal = new Vector3(1), UV = new Vector2(0,1) }
            ]);
        }

        public void Render()
        {
            MeshManager.UpdateMesh(_mesh, new MeshPart
            {
                Name = "physdebug",
                Vertices = _vertices.ToList(),
                Texture = "materials/error.mat"
            });

            _vertices.Clear();
        }
    }

    public class PhysicsDebugDrawFilter : BodyDrawFilter
    {
        protected override bool ShouldDraw(Body body)
        {
            if (ConVarStorage.Get<bool>("edt_enable") && ConVarStorage.Get<bool>("phys_debug"))
                return body.ObjectLayer == Layers.Moving;

            return false;
        }
    }
}