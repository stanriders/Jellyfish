using System;
using System.Collections.Generic;
using Jellyfish.Entities;

namespace Jellyfish;

public static class EntityManager
{
    private static readonly Dictionary<string, Type> entityClassDictionary = new();
    private static readonly List<BaseEntity> entityList = new();

    static EntityManager()
    {
        // TEMP
        AddClassName("npc_gman", typeof(Gman));
        AddClassName("bezierplane", typeof(BezierPlaneEntity));
        AddClassName("model_dynamic", typeof(DynamicModel));
        AddClassName("light_point", typeof(PointLight));
    }

    public static void AddClassName(string className, Type type)
    {
        entityClassDictionary.Add(className, type);
    }

    public static BaseEntity CreateEntity(string className)
    {
        if (entityClassDictionary.ContainsKey(className))
        {
            var type = entityClassDictionary[className];
            if (Activator.CreateInstance(type) is BaseEntity entity)
            {
                entityList.Add(entity);
                return entity;
            }
        }

        return null;
    }

    public static void Unload()
    {
        foreach (var entity in entityList)
            entity.Unload();
    }

    public static void Frame()
    {
        foreach (var entity in entityList)
            entity.Think();
    }
}