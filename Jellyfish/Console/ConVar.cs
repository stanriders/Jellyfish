namespace Jellyfish.Console;

public interface IConVar { }

public abstract class ConVar<TValue> : IConVar where TValue : notnull
{
    public string Name { get; set; }
    public TValue Value;

    protected ConVar(string name, TValue defaultValue = default!)
    {
        ConVarStorage.Add(name, this);
        Value = defaultValue;
        Name = name;
    }
}
