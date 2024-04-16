using System;

namespace Jellyfish;

public class EntityAttribute : Attribute
{
    public string ClassName { get; }

    public EntityAttribute(string className)
    {
        ClassName = className;
    }
}

