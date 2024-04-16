using System.IO;
using Jellyfish.Entities;
using OpenTK.Mathematics;
using YamlDotNet.Serialization;

namespace Jellyfish;

public static class MapParser
{
    private static readonly Deserializer Deserializer = new();

    public static void Parse(string path)
    {
        var mapString = File.ReadAllText(path);
        var map = Deserializer.Deserialize<Map>(mapString);
        foreach (var ent in map.Entities)
        {
            var entity = EntityManager.CreateEntity(ent.ClassName);
            if (entity == null)
            {
                //TODO: log?
                continue;
            }
            if (ent.Position != null)
                entity.Position = ent.Position.Value;

            if (ent.Rotation != null)
                entity.Rotation = ent.Rotation.Value;

            if (entity is DynamicModel model) 
                model.Model = ent.Model;

            if (entity is PointLight light)
            {
                if (ent.Color != null)
                    light.Color = ent.Color.ToColor4();

                if (ent.Enabled != null)
                    light.Enabled = ent.Enabled.Value;
            }

            if (entity is BezierPlaneEntity bezier)
            {
                if (ent.Size != null)
                    bezier.Size = ent.Size.Value;

                if (ent.Resolution != null)
                    bezier.Resolution = ent.Resolution.Value;
            }

            entity.Load();
        }
    }

    private class Map
    {
        public Entity[] Entities { get; set; } = null!;
        
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
            public required string ClassName { get; set; }
            public Vector3? Position { get; set; }

            public Vector3? Rotation { get; set; }

            public string? Model { get; set; }

            public MapColor4? Color { get; set; }

            public bool? Enabled { get; set; }

            public int? Resolution { get; set; }
            public Vector2? Size { get; set; }
        }
    }
}