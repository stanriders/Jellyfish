using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Jellyfish.Console;
using Jellyfish.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTK.Mathematics;

namespace Jellyfish;

public static class MapLoader
{
    private static readonly JsonConverter[] Converters = [new Color4Converter(), new Color3Converter(), new RotationConverter(), new Vector2Converter(), new Vector3Converter()];

    private const string maps_directory = "maps";

    public static string[] GetMapList()
    {
        if (!Directory.Exists(maps_directory))
            Directory.CreateDirectory(maps_directory);

        return Directory.EnumerateFiles(maps_directory, "*.json", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray()!;
    }

    public static void Load(string mapName)
    {
        Log.Context("MapLoader").Information("Parsing map {Path}...", mapName);

        var path = Path.Combine(maps_directory, $"{mapName}.json");

        if (!File.Exists(path))
        {
            Log.Context("MapLoader").Error("Map {Path} doesn't exist!", path);
            return;
        }

        var mapString = File.ReadAllText(path);
        var deserializer = JsonSerializer.CreateDefault(new JsonSerializerSettings {Converters = Converters});

        var map = JsonConvert.DeserializeObject<Map>(mapString, Converters);
        if (map == null)
        {
            Log.Context("MapLoader").Error("Couldn't parse map {Path}!", mapName);
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
                        if (entityProperty.Type.IsArray)
                        {
                            // TODO: arrays don't deserialize properly
                        }
                        else
                        {
                            var propertyValue = propertyToken.Value.ToObject(entityProperty.Type, deserializer);
                            entityProperty.SetValue(propertyValue);
                        }
                    }
                    else
                    {
                        if (entityProperty.Type == typeof(Quaternion))
                        {
                            // otherwise it initializes into NaNs
                            entityProperty.SetValue(Quaternion.Identity);
                        }
                    }
                }
            }

            entity.Load();
        }

        Log.Context("MapLoader").Information("Finished parsing map");
    }

    public static void Save(string mapName)
    {
        var path = Path.Combine(maps_directory, $"{mapName}.json");

        var serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings { Converters = Converters });

        var entities = new List<Map.Entity>();
        foreach (var entity in EntityManager.Entities!)
        {
            var entityAttribute = entity.GetType().GetCustomAttribute<EntityAttribute>();
            if (entityAttribute == null)
            {
                continue;
            }

            var properties = new List<Map.Property>();

            foreach (var entityProperty in entity.EntityProperties)
            {
                if (entityProperty.Value != entityProperty.DefaultValue)
                {
                    properties.Add(new Map.Property
                    {
                        Name = entityProperty.Name,
                        Value = JToken.FromObject(entityProperty.Value!, serializer)
                    });
                }
            }

            entities.Add(new Map.Entity
            {
                ClassName = entityAttribute.ClassName,
                Properties = properties.ToArray()
            });
        }

        var map = new Map
        {
            Entities = entities.ToArray()
        };

        var json = JsonConvert.SerializeObject(map, Formatting.Indented, Converters);
        File.WriteAllText(path, json);
    }

    public class Color4Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color4<Rgba>);
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not Color4<Rgba> color)
                return;

            new JObject
            {
                { "R", (int)(color.X * 255) },
                { "G", (int)(color.Y * 255) },
                { "B", (int)(color.Z * 255) },
                { "A", (int)(color.W * 255) }
            }.WriteTo(writer);
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
        public override bool CanWrite => true;
    }

    public class Color3Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color3<Rgb>);
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not Color3<Rgb> color)
                return;

            new JObject
            {
                { "R", (int)(color.X * 255) },
                { "G", (int)(color.Y * 255) },
                { "B", (int)(color.Z * 255) },
            }.WriteTo(writer);
        }
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            byte r = 0, g = 0, b = 0;

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
                }
            }

            return new Color3<Rgb>(r / 255.0f, g / 255.0f, b / 255.0f);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
    }

    public class RotationConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Quaternion);
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not Quaternion quaternion)
                return;

            var euler = quaternion.ToEulerAngles();

            new JObject
            {
                { "Pitch", MathHelper.RadiansToDegrees(euler.X) },
                { "Yaw", MathHelper.RadiansToDegrees(euler.Y) },
                { "Roll", MathHelper.RadiansToDegrees(euler.Z) }
            }.WriteTo(writer);
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
        public override bool CanWrite => true;
    }

    public class Vector2Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector2);
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not Vector2 vector)
                return;

            new JObject
            {
                { "X", vector.X },
                { "Y", vector.Y },
            }.WriteTo(writer);
        }
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            float x = 0, y = 0;

            while (reader.Read())
            {
                var token = reader.Path;
                switch (token)
                {
                    case "X":
                    {
                        reader.Read();
                        x = Convert.ToSingle(reader.Value);
                        break;
                    }
                    case "Y":
                    {
                        reader.Read();
                        y = Convert.ToSingle(reader.Value);
                        break;
                    }
                }
            }

            return new Vector2(x, y);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
    }

    public class Vector3Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not Vector3 vector)
                return;

            new JObject
            {
                { "X", vector.X },
                { "Y", vector.Y },
                { "Z", vector.Z }
            }.WriteTo(writer);
        }
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            float x = 0, y = 0, z = 0;

            while (reader.Read())
            {
                var token = reader.Path;
                switch (token)
                {
                    case "X":
                        {
                            reader.Read();
                            x = Convert.ToSingle(reader.Value);
                            break;
                        }
                    case "Y":
                        {
                            reader.Read();
                            y = Convert.ToSingle(reader.Value);
                            break;
                        }
                    case "Z":
                        {
                            reader.Read();
                            z = Convert.ToSingle(reader.Value);
                            break;
                        }
                }
            }

            return new Vector3(x, y, z);
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
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