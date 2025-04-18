﻿using System.Linq;
using JoltPhysicsSharp;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("model_static")]
public class StaticModel : BaseModelEntity, IPhysicsEntity
{
    private BodyID _physicsBodyId;

    public StaticModel()
    {
        AddProperty<string>("Model", editable: false);

        var position = GetProperty<Vector3>("Position");
        position!.Editable = false;

        var rotation = GetProperty<Quaternion>("Rotation");
        rotation!.Editable = false;
    }

    public override void Load()
    {
        ModelPath = $"models/{GetPropertyValue<string>("Model")}";
        base.Load();
        _physicsBodyId = PhysicsManager.AddStaticObject(Model!.Meshes.ToArray(), this) ?? 0;
    }

    public override void Unload()
    {
        PhysicsManager.RemoveObject(_physicsBodyId);
        base.Unload();
    }

    public void ResetVelocity()
    {
    }

    public void OnPhysicsPositionChanged(Vector3 position)
    {
    }

    public void OnPhysicsRotationChanged(Quaternion rotation)
    {
    }
}