using System;

namespace Jellyfish.Console;

[AttributeUsage(AttributeTargets.Method)]
public class ConCommandAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

public sealed class ConCommand
{
    public required string Name { get; init; }
    public required Action Callback { get; init; }
}