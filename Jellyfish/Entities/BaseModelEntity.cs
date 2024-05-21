using Jellyfish.Render;

namespace Jellyfish.Entities;

public abstract class BaseModelEntity : BaseEntity
{
    protected Model? Model { get; private set; }

    protected string? ModelPath { get; set; }

    public override void Load()
    {
        if (!string.IsNullOrEmpty(ModelPath))
            Model = new Model(ModelPath);

        base.Load();
    }
}