using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jellyfish.Console;

public static class ConVarStorage
{
    private static readonly Dictionary<string, IConVar> ConVars = new();

    static ConVarStorage()
    {
        var panels = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsPublic: true, IsAbstract: false } && typeof(IConVar).IsAssignableFrom(x));

        foreach (var panelType in panels)
        {
            if (Activator.CreateInstance(panelType) is not IConVar)
            {
                Log.Context("ConVarStorage").Error("Can't create convar {Type}", panelType.Name);
            }
        }
    }

    public static void Add(string name, IConVar convar)
    {
        if (!ConVars.TryAdd(name, convar))
            throw new Exception("Convar already exists!");
    }

    public static T? Get<T>(string name) where T : notnull
    {
        if (string.IsNullOrEmpty(name))
            return default;

        if (!ConVars.TryGetValue(name, out var convar))
            return default;

        return (convar as ConVar<T>)!.Value;
    }

    public static ConVar<T>? GetConVar<T>(string name) where T : notnull
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (!ConVars.TryGetValue(name, out var convar))
            return null;

        return convar as ConVar<T>;
    }

    public static void Set<T>(string name, T value) where T : notnull
    {
        if (string.IsNullOrEmpty(name))
            return;

        if (!ConVars.TryGetValue(name, out var convar))
            return;

        (convar as ConVar<T>)!.Value = value;
    }
}