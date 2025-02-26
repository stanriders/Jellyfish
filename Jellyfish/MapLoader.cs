using System;
using System.IO;
using System.Linq;
using Jellyfish.Console;
using Jellyfish.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;

namespace Jellyfish;

public static class MapLoader
{
    public static void Load(string path)
    {
        Log.Context("MapLoader").Information("Parsing map {Path}...", path);

        var mapString = File.ReadAllText(path);
        var deserializer = JsonSerializer.CreateDefault();
        deserializer.Converters.Add(new ColorConverter());
        deserializer.Converters.Add(new RotationConverter());

        var map = JsonConvert.DeserializeObject<Map>(mapString, new ColorConverter());
        if (map == null)
        {
            Log.Context("MapLoader").Error("Couldn't parse map {Path}!", path);
            return;
        }
        foreach (var ent in map.Entities)
        {
            var entity = EntityManager.CreateEntity(ent.ClassName);
            if (entity == null)
            {
                Log.Context("MapLoader").Warning("Couldn't create entity {Entity}", ent.ClassName);
                continue;
            }

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
                    else
                    {
                        if (entityProperty.Type == typeof(Quaternion))
                        {
                            // otherwise it initializes into NaNs
                            entityProperty.Value = Quaternion.Identity;
                        }
                    }
                }
            }

            entity.Load();
        }

        Log.Context("MapLoader").Information("Finished parsing map");
    }
    public class ColorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color4<Rgba>);
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

            return new Color4<Rgba>(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
        }

        public override bool CanRead => true;
        public override bool CanWrite => false;
    }

    public class RotationConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Quaternion);
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            float pitch = 0, yaw = 0, roll = 0;

            while (reader.Read())
            {
                var token = reader.Path;
                switch (token)
                {
                    case "Pitch":
                    {
                        reader.Read();
                        pitch = Convert.ToSingle(reader.Value);
                        break;
                    }
                    case "Yaw":
                    {
                        reader.Read();
                        yaw = Convert.ToSingle(reader.Value);
                        break;
                    }
                    case "Roll":
                    {
                        reader.Read();
                        roll = Convert.ToSingle(reader.Value);
                        break;
                    }
                }
            }

            return new Quaternion(MathHelper.DegreesToRadians(pitch), MathHelper.DegreesToRadians(yaw), MathHelper.DegreesToRadians(roll));
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

            public Property[]? Properties { get; set; }
        }
    }
}