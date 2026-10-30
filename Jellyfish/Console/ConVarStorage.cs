using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfish.Input;

namespace Jellyfish.Console;

public class ConVarStorage : IInputHandler
{
    private readonly Dictionary<string, IConVar> _conVars = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ConCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> ConVarNames => _conVars.Keys.ToList();
    public IReadOnlyList<string> ConCommandNames => _commands.Keys.ToList();

    public void Load()
    {
        Generated.ConVarRegistration.RegisterAll();
        RegisterClassConVars();
        RegisterConCommands();

        Engine.InputManager.RegisterInputHandler(this);
    }

    private void RegisterClassConVars()
    {
        var convars = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsPublic: true, IsAbstract: false, ContainsGenericParameters: false } &&
                        typeof(IConVar).IsAssignableFrom(x));

        foreach (var convarType in convars)
        {
            if (Activator.CreateInstance(convarType) is not IConVar)
            {
                Log.Context("ConVarStorage").Error("Can't create convar {Type}", convarType.Name);
            }
        }
    }

    private void RegisterConCommands()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var attribute = method.GetCustomAttribute<ConCommandAttribute>();
                if (attribute == null)
                    continue;

                var parameters = method.GetParameters();

                Action? callback = null;
                if (parameters.Length == 0)
                    callback = () => method.Invoke(null, null);

                if (callback == null)
                {
                    Log.Context(this).Error("Command {Name} ({Type}.{Method}) has an invalid signature", attribute.Name, type.Name, method.Name);
                    continue;
                }

                _commands[attribute.Name] = new ConCommand
                {
                    Name = attribute.Name,
                    Callback = callback,
                };
            }
        }
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

    public static bool CommandExists(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        return Engine.ConVarStorage._commands.ContainsKey(name);
    }

    public static void Set<T>(string name, T value) where T : notnull
    {
        if (string.IsNullOrEmpty(name))
            return;

        if (!Engine.ConVarStorage._conVars.TryGetValue(name, out var convar))
        {
            Log.Context("Console").Error("Unknown convar {Name}", name);
            return;
        }

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
        {
            Log.Context("Console").Error("Unknown convar {Name}", name);
            return;
        }

#if DEBUG
        if (convar.UntypedValue != value)
            Log.Context("Console").Debug("{Name} {Value}", name, value);
#endif

        convar.UntypedValue = value;
    }

    public static void Execute(string name)
    {
        if (string.IsNullOrEmpty(name))
            return;

        if (!Engine.ConVarStorage._commands.TryGetValue(name, out var command))
        {
            Log.Context("Console").Error("Unknown command {Name}", name);
            return;
        }

#if DEBUG
        Log.Context("Console").Debug("{Name}", name);
#endif

        command.Callback();
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