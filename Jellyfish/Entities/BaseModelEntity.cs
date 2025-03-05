using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

public abstract class BaseModelEntity : BaseEntity
{
    protected Model? Model { get; private set; }

    protected string? ModelPath { get; set; }

    public override void Load()
    {
        if (!string.IsNullOrEmpty(ModelPath))
        {
            Model = new Model(ModelPath)
            {
                Position = GetPropertyValue<Vector3>("Position"),
                Rotation = GetPropertyValue<Quaternion>("Rotation")
            };
        }

        base.Load();
    }

    public override void Unload()
    {
        Model?.Unload();

        base.Unload();
    }

    protected override void OnPositionChanged(Vector3 position)
    {
        if (Model != null)
        {
            Model.Position = position;
        }

        base.OnPositionChanged(position);
    }

    protected override void OnRotationChanged(Quaternion rotation)
    {
        if (Model != null)
        {
            Model.Rotation = rotation;
        }

        base.OnRotationChanged(rotation);
    }
}