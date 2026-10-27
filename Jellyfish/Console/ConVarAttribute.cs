using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Console;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ConVarAttribute(string name, object? defaultValue = null, Keys defaultBind = Keys.Unknown) : Attribute
{
    public string Name { get; } = name;
    public object? DefaultValue { get; } = defaultValue;
    public Keys DefaultBind { get; } = defaultBind;
}
