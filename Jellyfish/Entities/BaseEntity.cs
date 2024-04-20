using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Entities;

public abstract class BaseEntity
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public virtual IReadOnlyList<EntityProperty> EntityProperties { get; } = new List<EntityProperty>();

    public virtual void Load()
    {
    }

    public virtual void Unload()
    {
    }

    public virtual void Think()
    {
    }

    protected EntityProperty<T>? GetProperty<T>(string name)
    {
        if (EntityProperties.FirstOrDefault(x => x.Name == name) is EntityProperty<T> property) 
            return property;

        Log.Warning("Unknown property {Name}!", name);
        return null;
    }

    protected T? GetPropertyValue<T>(string name)
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