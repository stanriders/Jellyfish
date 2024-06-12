using System;

namespace Jellyfish.Entities;

public class EntityAction
{
    public string Name { get; set; } = null!;
    private Action Action { get; set; } = null!;
    public bool Enabled { get; set; }

    public EntityAction(string name, Action action, bool enabled)
    {
        Name = name;
        Action = action;
        Enabled = enabled;
    }

    public void Act()
    {
        if (Enabled)
        {
            Action.Invoke();
        }
    }
}