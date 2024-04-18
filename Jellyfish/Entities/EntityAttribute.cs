using System;

namespace Jellyfish.Entities;

public class EntityAttribute : Attribute
{
    public string ClassName { get; }

    public EntityAttribute(string className)
    {
        ClassName = className;
    }
}

