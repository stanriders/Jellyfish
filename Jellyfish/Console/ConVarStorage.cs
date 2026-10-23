using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfish.Input;

namespace Jellyfish.Console;

public class ConVarStorage : IInputHandler
{
    private readonly Dictionary<string, IConVar> _conVars = new();

    public IReadOnlyList<string> ConVarNames => _conVars.Keys.ToList();

    public void Load()
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

        Engine.InputManager.RegisterInputHandler(this);
    }

    public void Add(string name, IConVar convar)
    {
        if (!_conVars.TryAdd(name, convar))
            throw new Exception("Convar already exists!");
    }

    public static T? Get<T>(string name) where T : notnull
    {
        if (string.IsNullOrEmpty(name))
            return default;

        if (!Engine.ConVarStorage._conVars.TryGetValue(name, out var convar))
            return default;

        return (convar as ConVar<T>)!.Value;
    }

    public static ConVar<T>? GetConVar<T>(string name) where T : notnull
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (!Engine.ConVarStorage._conVars.TryGetValue(name, out var convar))
            return null;

        return convar as ConVar<T>;
    }

    public static IConVar? GetConVar(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        return Engine.ConVarStorage._conVars.GetValueOrDefault(name);
    }

    public static void Set<T>(string name, T value) where T : notnull
    {
        if (string.IsNullOrEmpty(name))
            return;

        if (!Engine.ConVarStorage._conVars.TryGetValue(name, out var convar))
            return;

        var convarTyped = convar as ConVar<T>;
#if DEBUG
        if (!convarTyped!.Value.Equals(value))
            Log.Context("Console").Debug("{Name} {Value}", name, value);
#endif

        convarTyped.Value = value;
    }

    public static void Set(string name, object value)
    {
        if (string.IsNullOrEmpty(name))
            return;

        if (!Engine.ConVarStorage._conVars.TryGetValue(name, out var convar))
            return;

#if DEBUG
        if (convar.UntypedValue != value)
            Log.Context("Console").Debug("{Name} {Value}", name, value);
#endif

        convar.UntypedValue = value;
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        foreach (var conVar in _conVars)
        {
            if (conVar.Value.Bind != null && keyboardState.IsKeyPressed(conVar.Value.Bind.Value))
            {
                Set(conVar.Key, !(bool)conVar.Value.UntypedValue);
                return true;
            }
        }

        return false;
    }
}