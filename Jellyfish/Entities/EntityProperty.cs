using System;

namespace Jellyfish.Entities;

public abstract class EntityProperty
{
    public string Name { get; set; } = null!;
    public Type Type { get; set; } = null!;

    private object? _value;
    public object? Value
    {
        get => _value;
        set
        {
            if (value != _value)
            {
                if (value != null && !value.Equals(_value))
                    OnChangeAction?.Invoke(value);

                _value = value;
            }
        }
    }

    public object? DefaultValue { get; set; }
    public bool Editable { get; set; } = true;
    public Action<object>? OnChangeAction { get; set; }
}

public class EntityProperty<T> : EntityProperty
{
    public EntityProperty(string name, T? defaultValue = default, bool editable = true, Action<T>? changeCallback = null)
    {
        Name = name;
        Type = typeof(T);
        Value = defaultValue;
        DefaultValue = defaultValue;
        Editable = editable;

        if (changeCallback != null)
            OnChangeAction = x => changeCallback((T)x);
    }
}