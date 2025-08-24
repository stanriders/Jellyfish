using System;

namespace Jellyfish.Console;

public interface IConVar
{
    Type Type { get; }
    object UntypedValue { get; set; }
}

public abstract class ConVar<TValue> : IConVar where TValue : notnull
{
    public string Name { get; set; }
    public TValue Value
    {
        get => (TValue) UntypedValue;
        set => UntypedValue = value;
    }

    protected ConVar(string name, TValue defaultValue = default!)
    {
        ConVarStorage.Add(name, this);
        UntypedValue = defaultValue;
        Name = name;
    }

    public Type Type => typeof(TValue);
    public object UntypedValue { get; set; }
}
