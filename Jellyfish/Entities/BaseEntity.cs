using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfish.Render;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Entities;

public abstract class BaseEntity
{
    private readonly Dictionary<string, EntityProperty> _entityProperties = new();
    public IReadOnlyList<EntityProperty> EntityProperties => _entityProperties.Values.ToList().AsReadOnly();

#if DEBUG
    public virtual bool DrawDevCone { get; set; }
    private EntityDevCone? _devCone;
#endif

    protected BaseEntity()
    {
        AddProperty("Name", Guid.NewGuid().ToString("n")[..8]);
        AddProperty<Vector3>("Position");
        AddProperty<Quaternion>("Rotation");
    }

    public virtual void Load()
    {
#if DEBUG
        if (DrawDevCone)
            _devCone = new EntityDevCone(this);
#endif
    }

    public virtual void Unload()
    {
    }

    public virtual void Think()
    {
#if DEBUG
        _devCone?.Think();
#endif
    }

    protected void AddProperty<T>(string name, T defaultValue = default!)
    {
        _entityProperties.Add(name, new EntityProperty<T>(name,defaultValue));
    }

    protected EntityProperty<T>? GetProperty<T>(string name)
    {
        if (_entityProperties.TryGetValue(name, out var property))
        {
            if (property is EntityProperty<T> castedProperty)
            {
                return castedProperty;
            }

            Log.Warning("Found property {Name} but it has different type!", name);
            return null;
        }

        Log.Warning("Unknown property {Name}!", name);
        return null;
    }

    public T? GetPropertyValue<T>(string name)
    {
        var property = GetProperty<T>(name);
        if (property != null)
        {
            var value = property.Value;
            if (value == null)
                return default;

            return (T?)property.Value;
        }

        Log.Warning("Unknown property {Name}!", name);
        return default;
    }

    public bool SetPropertyValue<T>(string name, T value)
    {
        var property = GetProperty<T>(name);
        if (property != null)
        {
            property.Value = value;
            return true;
        }

        return false;
    }
}

public class EntityDevCone
{
    private readonly BaseEntity _entity;
    private readonly Model _model;

    public EntityDevCone(BaseEntity entity)
    {
        _entity = entity;
        _model = new Model("models/spot_reference.smd", true);
    }

    public void Think()
    {
        _model.Position = _entity.GetPropertyValue<Vector3>("Position");
        _model.Rotation = _entity.GetPropertyValue<Quaternion>("Rotation");
        _model.Scale = new Vector3(0.3f);
        _model.ShouldDraw = _entity.DrawDevCone;
    }
}