using System;

namespace Jellyfish.Entities;

public abstract class EntityProperty
{
    public string Name { get; set; } = null!;
    public Type Type { get; set; } = null!;
    public object? Value { get; private set; }
    public object? DefaultValue { get; set; }
    public bool Editable { get; set; } = true;
    public Action<object>? OnChangeAction { get; set; }

    public void SetValue(object? value)
    {
        if (!Editable)
            throw new Exception("Value isn't editable!");

        if (value == Value)
            return;

        if (value != null && !value.Equals(Value))
            OnChangeAction?.Invoke(value);

        Value = value;
    }
}

public class EntityProperty<T> : EntityProperty
{
    public EntityProperty(string name, T? defaultValue = default, bool editable = true, Action<T>? changeCallback = null)
    {
        Name = name;
        Type = typeof(T);
        SetValue(defaultValue);
        DefaultValue = defaultValue;
        Editable = editable;

        if (changeCallback != null)
            OnChangeAction = x => changeCallback((T)x);
    }
}