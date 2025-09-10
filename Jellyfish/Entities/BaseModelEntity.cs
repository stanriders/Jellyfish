using System.IO;
using System.Linq;
using Jellyfish.Render;
using Jellyfish.Utils;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

public abstract class BaseModelEntity : BaseEntity
{
    protected Model? Model { get; set; }

    protected string? ModelPath { get; set; }

    protected BaseModelEntity() : base()
    {
        AddProperty("Animation", string.Empty, changeCallback: OnAnimationChange);
        AddProperty("Scale", Vector3.One, changeCallback: OnScaleChanged);
    }

    public override void Load()
    {
        if (!string.IsNullOrEmpty(ModelPath) && Path.Exists(ModelPath))
        {
            Model = ModelParser.Parse(ModelPath);
            Model.Position = GetPropertyValue<Vector3>("Position");
            Model.Rotation = GetPropertyValue<Quaternion>("Rotation");
            Model.Scale = GetPropertyValue<Vector3>("Scale");
        }

        GetProperty<string>("Animation")!.PossibleValues = Model?.Animations.Select(x => x.Name).Cast<object>().ToArray();

        var defaultAnimation = GetPropertyValue<string>("Animation");
        if (!string.IsNullOrEmpty(defaultAnimation))
        {
            var animation = Model?.Animations.FirstOrDefault(x => x.Name == defaultAnimation);
            if (animation != null)
            {
                Model?.Animator?.Play(animation);
                Model?.Animator?.Update(0);
            }
        }

        base.Load();
    }

    public override void Think(float frameTime)
    {
        Model?.Animator?.Update(frameTime);
        base.Think(frameTime);
    }

    public override void Unload()
    {
        Model?.Unload();

        base.Unload();
    }

    private void OnAnimationChange(string obj)
    {
        if (!Loaded)
            return;

        var animation = Model?.Animations.FirstOrDefault(x => x.Name == obj);
        if (animation != null)
        {
            Model?.Animator?.Play(animation);
            Model?.Animator?.Update(0);
        }
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

    public override BoundingBox? BoundingBox => Model?.BoundingBox;
}