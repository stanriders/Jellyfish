using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfish.Console;
using Jellyfish.Render;
using Jellyfish.Utils;
using OpenTK.Mathematics;
using Serilog;
using Log = Jellyfish.Console.Log;

namespace Jellyfish.Entities;

public class EnableDebugCones() : ConVar<bool>("edt_drawcones", true);
public class EnableEntityNames() : ConVar<bool>("edt_drawnames", false);

public abstract class BaseEntity
{
    private readonly Dictionary<string, EntityProperty> _entityProperties = new();
    public IReadOnlyList<EntityProperty> EntityProperties => _entityProperties.Values.ToList().AsReadOnly();

    private readonly Dictionary<string, EntityAction> _entityActions = new();
    public IReadOnlyList<EntityAction> EntityActions => _entityActions.Values.ToList().AsReadOnly();

    public string? Name => _entityProperties["Name"].Value as string;
    
    public virtual bool DrawDevCone { get; set; }

    public bool Loaded { get; private set; }
    public bool MarkedForDeath { get; private set; }

    protected BaseEntity()
    {
        AddProperty("Name", Guid.NewGuid().ToString("n")[..8], false);
        AddProperty("Position", Vector3.Zero, changeCallback: OnPositionChanged);
        AddProperty("Rotation", Quaternion.Identity, changeCallback: OnRotationChanged);

        AddAction("Kill", () => EntityManager.KillEntity(this));
    }

    public virtual void Load()
    {
        if (Loaded)
        {
            EntityLog().Error("Entity is already loaded!"); // todo? rename to init to make it less ambiguous? 
            return;
        }

        Loaded = true;
    }

    public virtual void Unload()
    {
        MarkedForDeath = true;
    }

    public virtual void Think()
    {
    }

    protected virtual void OnPositionChanged(Vector3 position) { }
    protected virtual void OnRotationChanged(Quaternion rotation) { }

    protected void AddProperty<T>(string name, T defaultValue = default!, bool editable = true, Action<T>? changeCallback = null)
    {
        _entityProperties.Add(name, new EntityProperty<T>(name, defaultValue, editable, changeCallback));
    }

    protected EntityProperty<T>? GetProperty<T>(string name)
    {
        if (_entityProperties.TryGetValue(name, out var property))
        {
            if (property is EntityProperty<T> castedProperty)
            {
                return castedProperty;
            }

            EntityLog().Warning("Found property {Name} but it has different type!", name);
            return null;
        }

        EntityLog().Warning("Unknown property {Name}!", name);
        return null;
    }

    public T? GetPropertyValue<T>(string name)
    {
        var property = GetProperty<T>(name);
        if (property != null)
        {
            var value = property.Value;
            if (value == null)
                return default;

            return (T?)property.Value;
        }

        EntityLog().Warning("Unknown property {Name}!", name);
        return default;
    }

    public bool SetPropertyValue<T>(string name, T value)
    {
        var property = GetProperty<T>(name);
        if (property != null)
        {
            property.SetValue(value);
            return true;
        }

        return false;
    }

    protected void AddAction(string name, Action action, bool enabled = true)
    {
        _entityActions.Add(name, new EntityAction(name, action, enabled));
    }

    protected EntityAction? GetAction(string name)
    {
        if (_entityActions.TryGetValue(name, out var action))
        {
            return action;
        }

        EntityLog().Warning("Unknown action {Name}!", name);
        return null;
    }

    protected ILogger EntityLog()
    {
        return Log.Context(Name ?? GetType().Name);
    }

    public virtual bool IsPointWithinBoundingBox(Vector3 point)
    {
        var position = GetPropertyValue<Vector3>("Position");
        return BoundingBox?.IsPointInside(point - position) ?? false;
    }

    public virtual BoundingBox? BoundingBox => DrawDevCone ? EntityManager.EntityDevCone.BoundingBox : null;

    public override string ToString()
    {
        return Name ?? GetType().Name;
    }
}