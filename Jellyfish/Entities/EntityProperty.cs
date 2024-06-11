using System;

namespace Jellyfish.Entities;

public abstract class EntityProperty
{
    public string Name { get; set; } = null!;
    public Type Type { get; set; } = null!;
    public object? Value { get; set; }
    public object? DefaultValue { get; set; }
    public bool Editable { get; set; } = true;
}

public class EntityProperty<T> : EntityProperty
{
    public EntityProperty(string name, T? defaultValue = default, bool editable = true)
    {
        Name = name;
        Type = typeof(T);
        Value = defaultValue;
        DefaultValue = defaultValue;
        Editable = editable;
    }
}