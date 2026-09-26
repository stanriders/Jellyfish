using System.Linq;
using JoltPhysicsSharp;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("model_static")]
public class StaticModel : BaseModelEntity, IPhysicsEntity
{
    private BodyID _physicsBodyId;

    public StaticModel()
    {
        AddProperty<string>("Model", editable: false, flags: EntityPropertyFlags.FilePath);

        var position = GetProperty<Vector3>("Position");
        position!.Editable = false;

        var rotation = GetProperty<Quaternion>("Rotation");
        rotation!.Editable = false;

        var scale = GetProperty<Vector3>("Scale");
        scale!.Editable = false;
    }

    public override void Load()
    {
        ModelPath = GetPropertyValue<string>("Model");
        base.Load();

        if (Model == null)
            return;

        _physicsBodyId = Engine.PhysicsManager.AddStaticObject(Model.Meshes.ToArray(), this) ?? 0;
    }

    public override void Unload()
    {
        Engine.PhysicsManager.RemoveObject(_physicsBodyId);
        base.Unload();
    }

    public void ResetVelocity()
    {
    }

    public void OnPhysicsChanged(Vector3 position, Quaternion rotation)
    {
    }
}