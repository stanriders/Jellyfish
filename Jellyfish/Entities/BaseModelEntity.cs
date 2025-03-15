using System.IO;
using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

public abstract class BaseModelEntity : BaseEntity
{
    protected Model? Model { get; private set; }

    protected string? ModelPath { get; set; }

    protected BaseModelEntity() : base()
    {
        AddProperty("Scale", Vector3.One, changeCallback: OnScaleChanged);
    }

    public override void Load()
    {
        if (!string.IsNullOrEmpty(ModelPath) && Path.Exists(ModelPath))
        {
            Model = new Model(ModelPath)
            {
                Position = GetPropertyValue<Vector3>("Position"),
                Rotation = GetPropertyValue<Quaternion>("Rotation"),
                Scale = GetPropertyValue<Vector3>("Scale")
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

    protected virtual void OnScaleChanged(Vector3 scale)
    {
        if (Model != null)
        {
            Model.Scale = scale;
        }
    }
}