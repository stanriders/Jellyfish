using System;

namespace Jellyfish.Entities;

public abstract class EntityProperty
{
    public string Name { get; set; }
    public Type Type { get; set; }
    public object? Value { get; set; }
    public object? DefaultValue { get; set; }
}

public class EntityProperty<T> : EntityProperty
{
    public EntityProperty(string name, T? defaultValue = default)
    {
        Name = name;
        Type = typeof(T);
        Value = defaultValue;
        DefaultValue = defaultValue;
    }
}