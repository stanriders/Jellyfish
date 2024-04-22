using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

public abstract class BaseModelEntity : BaseEntity
{
    private Model? _model;

    protected string? ModelPath { get; set; }

    public override void Load()
    {
        if (!string.IsNullOrEmpty(ModelPath))
            _model = new Model(ModelPath);

        base.Load();
    }

    public override void Think()
    {
        if (_model != null)
        {
            _model.Position = GetPropertyValue<Vector3>("Position");
            _model.Rotation = GetPropertyValue<Vector3>("Rotation");
        }

        base.Think();
    }
}