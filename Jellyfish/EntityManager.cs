using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Serilog;

namespace Jellyfish;

public static class EntityManager
{
    private static readonly Dictionary<string, Type> EntityClassDictionary = new();
    private static readonly List<BaseEntity> EntityList = new();

    public static IReadOnlyList<BaseEntity> Entities => EntityList.AsReadOnly();

    public static void Load()
    {
        var entities = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsPublic: true, IsAbstract: false } && x.IsSubclassOf(typeof(BaseEntity)));

        foreach (var entityType in entities)
        {
            var entityAttribute = entityType.GetCustomAttribute<EntityAttribute>();
            if (entityAttribute == null)
            {
                Log.Error("Invalid entity declaration for type {Type}", entityType.FullName);
                continue;
            }

            if (EntityClassDictionary.ContainsKey(entityType.Name))
            {
                Log.Error("Duplicate class name {Name} for type {Type}", entityAttribute.ClassName, entityType.FullName);
                continue;
            }

            Log.Information("Registering class name {Name} for type {Type}...", entityAttribute.ClassName, entityType.FullName);
            EntityClassDictionary.Add(entityAttribute.ClassName, entityType);
        }
    }

    public static BaseEntity? CreateEntity(string className)
    {
        if (EntityClassDictionary.TryGetValue(className, out var type))
        {
            Log.Information("Creating entity {Name}...", className);
            if (Activator.CreateInstance(type) is BaseEntity entity)
            {
                EntityList.Add(entity);
                return entity;
            }
        }

        Log.Error("Tried to create unknown entity {Name}!", className);
        return null;
    }

    public static void Unload()
    {
        foreach (var entity in EntityList)
            entity.Unload();
    }

    public static void Frame()
    {
        foreach (var entity in EntityList)
            entity.Think();
    }

    public static BaseEntity? FindEntity(string className)
    {
        if (!EntityClassDictionary.TryGetValue(className, out var entityType))
        {
            Log.Error("Class name {Name} doesn't exist!", className);
            return null;
        }

        var entity = EntityList.FirstOrDefault(x => x.GetType() == entityType);
        if (entity != null)
        {
            return entity;
        }

        Log.Error("Entity {Name} wasn't found", className);
        return null;
    }
}