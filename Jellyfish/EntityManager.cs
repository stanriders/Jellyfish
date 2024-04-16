using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jellyfish;

public static class EntityManager
{
    private static readonly Dictionary<string, Type> EntityClassDictionary = new();
    private static readonly List<BaseEntity> EntityList = new();

    public static void Load()
    {
        var entities = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x.IsPublic && x.IsSubclassOf(typeof(BaseEntity)));

        foreach (var entityType in entities)
        {
            var entityAttribute = entityType.GetCustomAttribute<EntityAttribute>();
            if (entityAttribute == null)
            {
                // TODO: log?
                continue;
            }

            if (!EntityClassDictionary.ContainsKey(entityType.Name))
            {
                // TODO: log?
                EntityClassDictionary.Add(entityAttribute.ClassName, entityType);
            }
        }
    }

    public static BaseEntity CreateEntity(string className)
    {
        if (EntityClassDictionary.ContainsKey(className))
        {
            var type = EntityClassDictionary[className];
            if (Activator.CreateInstance(type) is BaseEntity entity)
            {
                EntityList.Add(entity);
                return entity;
            }
        }

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
}