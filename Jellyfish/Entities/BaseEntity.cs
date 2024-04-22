using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Entities;

public abstract class BaseEntity
{
    private readonly Dictionary<string, EntityProperty> _entityProperties = new();
    public IReadOnlyList<EntityProperty> EntityProperties => _entityProperties.Values.ToList().AsReadOnly();

    protected BaseEntity()
    {
        AddProperty("Name", Guid.NewGuid().ToString("n")[..8]);
        AddProperty<Vector3>("Position");
        AddProperty<Vector3>("Rotation");
    }

    public virtual void Load()
    {
    }

    public virtual void Unload()
    {
    }

    public virtual void Think()
    {
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

        Log.Warning("Unknown property {Name}!", name);
        return false;
    }
}