using System;
using System.IO;
using System.Linq;
using Jellyfish.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish;

public static class MapLoader
{
    public static void Load(string path)
    {
        Log.Information("[MapLoader] Parsing map {Path}...", path);

        var mapString = File.ReadAllText(path);
        var deserializer = JsonSerializer.CreateDefault();
        deserializer.Converters.Add(new ColorConverter());

        var map = JsonConvert.DeserializeObject<Map>(mapString, new ColorConverter());
        if (map == null)
        {
            Log.Error("[MapLoader] Couldn't parse map {Path}!", path);
            return;
        }
        foreach (var ent in map.Entities)
        {
            var entity = EntityManager.CreateEntity(ent.ClassName);
            if (entity == null)
            {
                Log.Warning("[MapLoader] Couldn't create entity {Entity}", ent.ClassName);
                continue;
            }

            if (ent.Position != null)
                entity.Position = ent.Position.Value;

            if (ent.Rotation != null)
                entity.Rotation = ent.Rotation.Value;

            if (ent.Properties != null)
            {
                foreach (var entityProperty in entity.EntityProperties)
                {
                    var propertyToken = ent.Properties.FirstOrDefault(x=> x.Name == entityProperty.Name);
                    if (propertyToken != null)
                    {
                        var propertyValue = propertyToken.Value.ToObject(entityProperty.Type, deserializer);
                        entityProperty.Value = propertyValue;
                    }
                }
            }

            entity.Load();
        }

        Log.Information("[MapLoader] Finished parsing map");
    }
    public class ColorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color4);
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            byte r = 0, g = 0, b = 0, a = 0;

            while (reader.Read())
            {
                var token = reader.Path;
                switch (token)
                {
                    case "R":
                    {
                        reader.Read();
                        r = Convert.ToByte(reader.Value);
                        break;
                    }
                    case "G":
                    {
                        reader.Read();
                        g = Convert.ToByte(reader.Value);
                        break;
                    }
                    case "B":
                    {
                        reader.Read();
                        b = Convert.ToByte(reader.Value);
                        break;
                    }
                    case "A":
                    {
                        reader.Read();
                        a = Convert.ToByte(reader.Value);
                        break;
                    }
                }
            }

            return new Color4(r, g, b, a);
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
    }

    private class Map
    {
        public Entity[] Entities { get; set; } = null!;

        public class Property
        {
            public string Name { get; set; } = null!;
            public JToken Value { get; set; } = null!;
        }

        public class Entity
        {
            public required string ClassName { get; set; }
            public Vector3? Position { get; set; }
            public Vector3? Rotation { get; set; }

            public Property[]? Properties { get; set; }
        }
    }
}