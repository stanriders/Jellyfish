using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Console;

public interface IConVar
{
    Type Type { get; }
    Keys? Bind { get; }
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

    protected ConVar(string name, TValue defaultValue = default!, Keys? defaultBind = null)
    {
        ConVarStorage.Add(name, this);
        UntypedValue = defaultValue;
        Name = name;
        Bind = defaultBind;

        if (Bind != null && Type != typeof(bool))
            throw new ArgumentException("Can't have a bind for non-bool convars");
    }

    public Type Type => typeof(TValue);
    public Keys? Bind { get; }
    public object UntypedValue { get; set; }
}
