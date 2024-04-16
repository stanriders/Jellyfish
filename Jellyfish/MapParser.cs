using System.IO;
using Jellyfish.Entities;
using OpenTK.Mathematics;
using YamlDotNet.Serialization;

namespace Jellyfish;

public static class MapParser
{
    public static void Parse(string path)
    {
        var mapString = File.ReadAllText(path);
        var deserializer = new Deserializer();
        var map = deserializer.Deserialize<Map>(mapString);
        foreach (var ent in map.Entities)
        {
            var entity = EntityManager.CreateEntity(ent.ClassName);
            entity.Position = ent.Position;
            entity.Rotation = ent.Rotation;

            if (entity is DynamicModel model) 
                model.Model = ent.Model;

            if (entity is PointLight light)
            {
                light.Color = ent.Color.ToColor4();
                light.Enabled = ent.Enabled;
            }

            entity.Load();
        }
    }

    private class Map
    {
        public Entity[] Entities { get; set; }
        
        // FIXME: replace with some kind of a deserialization extension?
        public class MapColor4
        {
            public byte R { get; set; }
            public byte G { get; set; }
            public byte B { get; set; }
            public byte A { get; set; }

            public Color4 ToColor4()
            {
                return new Color4(R, G, B, A);
            }
        }

        public class Entity
        {
            public string ClassName { get; set; }
            public Vector3 Position { get; set; }

            public Vector3 Rotation { get; set; }

            public string Model { get; set; }

            public MapColor4 Color { get; set; }

            public bool Enabled { get; set; }
        }
    }
}