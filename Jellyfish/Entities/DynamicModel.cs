using System.Collections.Generic;

namespace Jellyfish.Entities;

[Entity("model_dynamic")]
public class DynamicModel : BaseModelEntity
{
    public override IReadOnlyList<EntityProperty> EntityProperties { get; } = new List<EntityProperty>
    {
        new EntityProperty<string>("Model"),
    };

    public override void Load()
    {
        ModelPath = $"models/{GetPropertyValue<string>("Model")}";
        base.Load();
    }
}