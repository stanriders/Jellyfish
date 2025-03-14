using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfish.Render;
using OpenTK.Mathematics;
using Serilog;
using Log = Jellyfish.Console.Log;

namespace Jellyfish.Entities;

public abstract class BaseEntity
{
    private readonly Dictionary<string, EntityProperty> _entityProperties = new();
    public IReadOnlyList<EntityProperty> EntityProperties => _entityProperties.Values.ToList().AsReadOnly();

    private readonly Dictionary<string, EntityAction> _entityActions = new();
    public IReadOnlyList<EntityAction> EntityActions => _entityActions.Values.ToList().AsReadOnly();

    public string? Name => _entityProperties["Name"].Value as string;
    
#if DEBUG
    public virtual bool DrawDevCone { get; set; }
    private EntityDevCone? _devCone;
#endif

    public bool Loaded { get; private set; }

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

#if DEBUG
        if (DrawDevCone)
            _devCone = new EntityDevCone(this);
#endif
        Loaded = true;
    }

    public virtual void Unload()
    {
#if DEBUG
        if (DrawDevCone && _devCone != null)
        {
            _devCone.Unload();
            _devCone = null;
        }
#endif
    }

    public virtual void Think()
    {
#if DEBUG
        _devCone?.Think();
#endif
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
}

public class EntityDevCone
{
    private readonly BaseEntity _entity;
    private readonly Model _model;

    public EntityDevCone(BaseEntity entity)
    {
        _entity = entity;
        _model = new Model("models/spot_reference.smd", true);
    }

    public void Think()
    {
        _model.Position = _entity.GetPropertyValue<Vector3>("Position");
        _model.Rotation = _entity.GetPropertyValue<Quaternion>("Rotation");
        _model.Scale = _entity.GetPropertyValue<Vector3>("Scale") * new Vector3(0.3f);
        _model.ShouldDraw = _entity.DrawDevCone;
    }

    public void Unload()
    {
        _model.Unload();
    }
}