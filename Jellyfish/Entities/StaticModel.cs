using System.Linq;

namespace Jellyfish.Entities;

[Entity("model_static")]
public class StaticModel : BaseModelEntity
{
    public StaticModel()
    {
        AddProperty<string>("Model");
    }

    public override void Load()
    {
        ModelPath = $"models/{GetPropertyValue<string>("Model")}";
        base.Load();
        PhysicsManager.AddStaticObject(Model!.Meshes.Select(x => x.MeshPart).ToArray(), this);
    }
}