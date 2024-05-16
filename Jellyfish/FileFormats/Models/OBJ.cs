using Jellyfish.Render;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenTK.Mathematics;

namespace Jellyfish.FileFormats.Models;

public class OBJ
{
    public static MeshPart[] Load(string path)
    {
        var mesh = new MeshPart
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Texture = "eye.jpg"
        };

        var file = File.ReadAllText(path);
        using (var reader = new StringReader(file))
        {
            var currentVertex = new Vertex();
            while (reader.Peek() != -1)
            {
                var line = reader.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    var split = line.Split(' ').Take(4).ToArray();
                    if (split[0] == "v")
                    {
                        mesh.Vertices.Add(currentVertex);

                        currentVertex = new Vertex();
                        //v <float|PosX PosY PosZ>
                        currentVertex.Coordinates = new Vector3(Convert.ToSingle(split[1], CultureInfo.InvariantCulture),
                            Convert.ToSingle(split[2], CultureInfo.InvariantCulture),
                            Convert.ToSingle(split[3], CultureInfo.InvariantCulture));
                    }

                    if (split[0] == "vt")
                    {
                        //vt <float|PosX PosY PosZ>
                        currentVertex.UV = new Vector2(Convert.ToSingle(split[1], CultureInfo.InvariantCulture),
                            Convert.ToSingle(split[2], CultureInfo.InvariantCulture));
                    }
                    else if (split[0] == "vn")
                    {
                        //vn <float|PosX PosY PosZ>
                        currentVertex.Normal = new Vector3(Convert.ToSingle(split[1], CultureInfo.InvariantCulture),
                            Convert.ToSingle(split[2], CultureInfo.InvariantCulture),
                            Convert.ToSingle(split[3], CultureInfo.InvariantCulture));
                    }
                    else if (split[0] == "f")
                    {
                        if (mesh.Indices == null)
                            mesh.Indices = new List<uint>();

                        //f <int|vertex/uv/normal vertex/uv/normal vertex/uv/normal>
                        var x = Convert.ToUInt32(split[1].Split('/')[0]);
                        mesh.Indices.Add(x);
                        var y = Convert.ToUInt32(split[2].Split('/')[0]);
                        mesh.Indices.Add(y);
                        if (split.Length > 2)
                        {
                            var z = Convert.ToUInt32(split[3].Split('/')[0]);
                            mesh.Indices.Add(z);
                        }
                    }
                }
            }
        }

        return new[] { mesh };
    }

}