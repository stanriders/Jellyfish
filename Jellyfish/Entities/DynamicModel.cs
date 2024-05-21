using System;
using JoltPhysicsSharp;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("model_dynamic")]
public class DynamicModel : BaseModelEntity
{
    private BodyID _physicsBodyId;
    private Vector3? _previousPosition;
    private Vector3? _previousRotation;

    // TODO: something better than datetime
    private DateTime _lastUpdate = DateTime.Now;

    public DynamicModel()
    {
        AddProperty<string>("Model");
    }

    public override void Load()
    {
        ModelPath = $"models/{GetPropertyValue<string>("Model")}";
        base.Load();

        _physicsBodyId = PhysicsManager.AddDynamicObject(this);
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
}