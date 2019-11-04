
using System.IO;
using Jellyfish.Entities;
using OpenTK;

namespace Jellyfish
{
    public static class MapParser
    {
        private class Map
        {
            public class MapVector3
            {
                public float X { get; set; }
                public float Y { get; set; }
                public float Z { get; set; }

                public MapVector3()
                {
                }

                public MapVector3(Vector3 vec)
                {
                    X = vec.X;
                    Y = vec.Y;
                    Z = vec.Z;
                }

                public Vector3 ToVector3()
                {
                    return new Vector3(X, Y, Z);
                }
            }

            public class Entity
            {
                public string ClassName { get; set; }
                public MapVector3 Position { get; set; }

                public MapVector3 Rotation { get; set; }

                public string Model { get; set; }
            }

            public Entity[] Entities { get; set; }
        }

        public static void Parse(string path)
        {
            var mapString = File.ReadAllText(path);
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var map = deserializer.Deserialize<Map>(mapString);
            foreach (var ent in map.Entities)
            {
                var entity = EntityManager.CreateEntity(ent.ClassName);
                entity.Position = ent.Position.ToVector3();
                entity.Rotation = ent.Rotation.ToVector3();

                if (entity is DynamicModel model)
                {
                    model.Model = ent.Model;
                    model.Load();
                }
            }
        }
    }
}
