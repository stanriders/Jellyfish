using System;

namespace Jellyfish.Entities;

public class EntityAttribute(string className) : Attribute
{
    public string ClassName { get; } = className;
}

