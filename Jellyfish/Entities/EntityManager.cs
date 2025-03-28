﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfish.Console;

namespace Jellyfish.Entities;

public class EntityManager
{
    private readonly Dictionary<string, Type> _entityClassDictionary = new();
    private readonly List<BaseEntity> _entityList = new();
    private readonly Queue<BaseEntity> _killQueue = new();

    public static IReadOnlyList<BaseEntity>? Entities => instance?._entityList.AsReadOnly();
    public static IReadOnlyList<string>? EntityClasses => instance?._entityClassDictionary.Keys.ToList().AsReadOnly();

    private static EntityManager? instance;

    public EntityManager()
    {
        instance = this;
        Load();
    }

    public void Load()
    {
        var entities = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsPublic: true, IsAbstract: false } && x.IsSubclassOf(typeof(BaseEntity)));

        foreach (var entityType in entities)
        {
            var entityAttribute = entityType.GetCustomAttribute<EntityAttribute>();
            if (entityAttribute == null)
            {
                Log.Context(this).Error("Invalid entity declaration for type {Type}", entityType.FullName);
                continue;
            }

            if (_entityClassDictionary.ContainsKey(entityType.Name))
            {
                Log.Context(this).Error("Duplicate class name {Name} for type {Type}", entityAttribute.ClassName, entityType.FullName);
                continue;
            }

            Log.Context(this).Information("Registering class name {Name} for type {Type}...", entityAttribute.ClassName, entityType.FullName);
            _entityClassDictionary.Add(entityAttribute.ClassName, entityType);
        }
    }

    public void Unload()
    {
        foreach (var entity in _entityList)
        {
            entity.Unload();
        }

        _entityList.Clear();
    }

    public void Frame()
    {
        while (_killQueue.Count > 0)
        {
            var entity = _killQueue.Dequeue();

            Log.Context(this).Information("Destroying entity {Name}...", entity.GetPropertyValue<string>("Name"));
            entity.Unload();
            _entityList.Remove(entity);
        }

        foreach (var entity in _entityList)
            entity.Think();
    }

    public static BaseEntity? CreateEntity(string className)
    {
        if (instance == null)
        {
            Log.Context("EntityManager").Information("Entity manager doesn't exist");
            return null;
        }

        if (instance._entityClassDictionary.TryGetValue(className, out var type))
        {
            Log.Context("EntityManager").Information("Creating entity {Name}...", className);
            if (Activator.CreateInstance(type) is BaseEntity entity)
            {
                instance._entityList.Add(entity);
                return entity;
            }
        }

        Log.Context("EntityManager").Error("Tried to create unknown entity {Name}!", className);
        return null;
    }

    public static BaseEntity? FindEntity(string className, bool silent = false)
    {
        if (instance == null)
        {
            Log.Context("EntityManager").Information("Entity manager doesn't exist");
            return null;
        }

        if (!instance._entityClassDictionary.TryGetValue(className, out var entityType))
        {
            Log.Context("EntityManager").Error("Class name {Name} doesn't exist!", className);
            return null;
        }

        var entity = instance._entityList.FirstOrDefault(x => x.GetType() == entityType);
        if (entity != null)
        {
            return entity;
        }

        if (!silent)
            Log.Context("EntityManager").Error("Entity {Name} wasn't found", className);

        return null;
    }

    public static BaseEntity? FindEntityByName(string? name, bool silent = false)
    {
        if (instance == null)
        {
            Log.Context("EntityManager").Information("Entity manager doesn't exist");
            return null;
        }

        if (name == null)
            return null;

        var entity = instance._entityList.FirstOrDefault(x => x.Name == name);
        if (entity != null)
        {
            return entity;
        }

        if (!silent)
            Log.Context("EntityManager").Error("Entity {Name} wasn't found", name);

        return null;
    }

    public static void KillEntity(BaseEntity entity)
    {
        if (instance == null)
        {
            Log.Context("EntityManager").Information("Entity manager doesn't exist");
            return;
        }
        
        if (!instance._entityList.Any(x => x == entity))
        {
            Log.Context("EntityManager").Error("Trying to kill entity {Name} that doesn't exist already???", entity.GetPropertyValue<string>("Name"));
            return;
        }
        
        // there are some entity list enumerations that run every frame so we want to kill entities on the start of the frame instead of the middle of it
        instance._killQueue.Enqueue(entity);
    }
}