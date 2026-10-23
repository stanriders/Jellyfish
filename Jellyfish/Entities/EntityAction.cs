using System;

namespace Jellyfish.Entities;

public class EntityAction(string name, Action action, bool enabled)
{
    public string Name { get; } = name;
    private Action Action { get; } = action;
    public bool Enabled { get; } = enabled;

    public void Act()
    {
        if (Enabled)
        {
            Action.Invoke();
        }
    }
}