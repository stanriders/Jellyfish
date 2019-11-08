
using System.IO;
using Jellyfish.Entities;
using OpenTK;
using OpenTK.Graphics;

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

            public class MapColor4
            {
                public byte R { get; set; }
                public byte G { get; set; }
                public byte B { get; set; }
                public byte A { get; set; }

                public MapColor4()
                {
                }

                public MapColor4(Color4 vec)
                {
                    R = (byte) (vec.R * 255);
                    G = (byte) (vec.G * 255);
                    B = (byte) (vec.B * 255);
                    A = (byte) (vec.A * 255);
                }

                public Color4 ToColor4()
                {
                    return new Color4(R, G, B, A);
                }
            }

            public class Entity
            {
                public string ClassName { get; set; }
                public MapVector3 Position { get; set; }

                public MapVector3 Rotation { get; set; }

                public string Model { get; set; }

                public MapColor4 Color { get; set; }

                public bool Enabled { get; set; }
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
                entity.Position = ent.Position?.ToVector3() ?? new Vector3(0, 0, 0);
                entity.Rotation = ent.Rotation?.ToVector3() ?? new Vector3(0,0,0);

                if (entity is DynamicModel model)
                {
                    model.Model = ent.Model;
                }

                if (entity is PointLight light)
                {
                    light.Color = ent.Color.ToColor4();
                    light.Enabled = ent.Enabled;
                }

                entity.Load();
            }
        }
    }
}
