using System;
using System.Linq;

namespace Jellyfish.Entities;

[Flags]
public enum EntityPropertyFlags
{
    None, 
    FilePath
}

public abstract class EntityProperty
{
    public string Name { get; protected init; } = null!;
    public Type Type { get; protected init; } = null!;
    public object? Value { get; private set; }
    public object? DefaultValue { get; protected init; }
    public object[]? PossibleValues { get; set; }
    public bool Editable { get; set; } = true;
    public bool ShowGizmo { get; protected init; }
    public EntityPropertyFlags Flags { get; protected init; }
    public Action<object>? OnChangeAction { get; init; }

    public void SetValue(object? value)
    {
        if (value == Value)
            return;

        var oldValue = Value;
        Value = value;

        if (value != null && !value.Equals(oldValue))
            OnChangeAction?.Invoke(value);
    }

    public override string ToString()
    {
        return $"{Name} - {Value}";
    }
}

public class EntityProperty<T> : EntityProperty
{
    public EntityProperty(string name, T? defaultValue = default, T[]? possibleValues = null, bool editable = true, bool showGizmo = false, Action<T>? changeCallback = null, EntityPropertyFlags flags = EntityPropertyFlags.None)
    {
        Name = name;
        Type = typeof(T);
        SetValue(defaultValue);
        DefaultValue = defaultValue;
        PossibleValues = possibleValues?.Cast<object>().ToArray();
        Editable = editable;
        ShowGizmo = showGizmo;
        Flags = flags;

        if (changeCallback != null)
            OnChangeAction = x => changeCallback((T)x);
    }
}