using System;

namespace Jellyfish.Entities;

public abstract class EntityProperty
{
    public string Name { get; set; } = null!;
    public Type Type { get; set; } = null!;
    public object? Value { get; private set; }
    public object? DefaultValue { get; set; }
    public bool Editable { get; set; } = true;
    public bool ShowGizmo { get; set; } = false;
    public Action<object>? OnChangeAction { get; set; }

    public void SetValue(object? value)
    {
        // TODO: enable after excluding map loader
        //if (!Editable)
        //    throw new Exception("Value isn't editable!");

        if (value == Value)
            return;

        Value = value;

        if (value != null)
            OnChangeAction?.Invoke(value);
    }

    public override string ToString()
    {
        return $"{Name} - {Value}";
    }
}

public class EntityProperty<T> : EntityProperty
{
    public EntityProperty(string name, T? defaultValue = default, bool editable = true, bool showGizmo = false, Action<T>? changeCallback = null)
    {
        Name = name;
        Type = typeof(T);
        SetValue(defaultValue);
        DefaultValue = defaultValue;
        Editable = editable;
        ShowGizmo = showGizmo;

        if (changeCallback != null)
            OnChangeAction = x => changeCallback((T)x);
    }
}