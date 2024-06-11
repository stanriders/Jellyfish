using System.Linq;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("model_static")]
public class StaticModel : BaseModelEntity
{
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
        PhysicsManager.AddStaticObject(Model!.Meshes.Select(x => x.MeshPart).ToArray(), this);
    }
}