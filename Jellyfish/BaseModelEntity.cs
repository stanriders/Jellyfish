using Jellyfish.Render;

namespace Jellyfish;

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
            _model.Position = Position;
            _model.Rotation = Rotation;
        }

        base.Think();
    }
}