using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenTK;

namespace Jellyfish
{
    public class MeshInfo
    {
        public string Texture { get; set; }
        public List<Vector3> Vertices { get; set; } = new List<Vector3>();
        public List<Vector2> UVs { get; set; } = new List<Vector2>();
        public List<Vector3> Normals { get; set; } = new List<Vector3>();
        public List<uint> Indices { get; set; } // can be null
    }

    public static class ModelParser
    {
        public static MeshInfo[] Parse(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".smd": 
                    return ParseSMD(path);
                case ".obj":
                    return ParseOBJ(path);
                default: 
                    return null;
            }
        }

        private static MeshInfo[] ParseSMD(string path)
        {
            var meshes = new Dictionary<string, MeshInfo>();

            var file = File.ReadAllText(path);
            using (var reader = new StringReader(file))
            {
                bool shouldParse = false;
                string currentTexture = string.Empty;
                while (reader.Peek() != -1)
                {
                    var line = reader.ReadLine();
                    if (line != null)
                    {
                        if (line == "triangles" && !shouldParse)
                        {
                            shouldParse = true;
                            continue;
                        }
                        else if (line == "end" && shouldParse)
                        {
                            shouldParse = false;
                            continue;
                        }

                        if (shouldParse)
                        {
                            var split = line.Trim().Split(' ').Where(x=> !string.IsNullOrEmpty(x)).ToArray();
                            if (int.TryParse(split[0], out _))
                            {
                                //<int|Parent bone> <float|PosX PosY PosZ> <normal|NormX NormY NormZ> <normal|U V> <int|links> <int|Bone ID> <normal|Weight> [...]
                                var data = split.Take(9).ToArray();
                                var vertex = new Vector3(Convert.ToSingle(data[1], CultureInfo.InvariantCulture),
                                    Convert.ToSingle(data[2], CultureInfo.InvariantCulture),
                                    Convert.ToSingle(data[3], CultureInfo.InvariantCulture));

                                var normal = new Vector3(Convert.ToSingle(data[4], CultureInfo.InvariantCulture),
                                    Convert.ToSingle(data[5], CultureInfo.InvariantCulture),
                                    Convert.ToSingle(data[6], CultureInfo.InvariantCulture));

                                var uv = new Vector2(Convert.ToSingle(data[7], CultureInfo.InvariantCulture),
                                    Convert.ToSingle(data[8], CultureInfo.InvariantCulture));

                                meshes[currentTexture].Vertices.Add(vertex);
                                meshes[currentTexture].UVs.Add(uv);
                                meshes[currentTexture].Normals.Add(normal);
                            }
                            else
                            {
                                // we split model by textures 
                                if (!meshes.ContainsKey(line))
                                    meshes.Add(line, new MeshInfo() { Texture = line });

                                currentTexture = line;
                            }
                        }
                    }
                }
            }

            return meshes.Values.ToArray();
        }
        
        private static MeshInfo[] ParseOBJ(string path)
        {
            var meshes = new Dictionary<string, MeshInfo>();
            meshes.Add("a", new MeshInfo());
            meshes["a"].Texture = "eye.jpg";

            var file = File.ReadAllText(path);
            using (var reader = new StringReader(file))
            {
                while (reader.Peek() != -1)
                {
                    var line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var split = line.Split(' ').Take(4).ToArray();
                        if (split[0] == "v")
                        {
                            //v <float|PosX PosY PosZ>
                            var vertex = new Vector3(Convert.ToSingle(split[1], CultureInfo.InvariantCulture),
                                Convert.ToSingle(split[2], CultureInfo.InvariantCulture),
                                Convert.ToSingle(split[3], CultureInfo.InvariantCulture));

                            meshes["a"].Vertices.Add(vertex);
                        }
                        if (split[0] == "vt")
                        {
                            //vt <float|PosX PosY PosZ>
                            var uv = new Vector2(Convert.ToSingle(split[1], CultureInfo.InvariantCulture),
                                Convert.ToSingle(split[2], CultureInfo.InvariantCulture));

                            meshes["a"].UVs.Add(uv);
                        }
                        else if (split[0] == "vn")
                        {
                            //vn <float|PosX PosY PosZ>
                            var normal = new Vector3(Convert.ToSingle(split[1], CultureInfo.InvariantCulture),
                                Convert.ToSingle(split[2], CultureInfo.InvariantCulture),
                                Convert.ToSingle(split[3], CultureInfo.InvariantCulture));

                            meshes["a"].Normals.Add(normal);
                        }
                        else if(split[0] == "f")
                        {
                            if (meshes["a"].Indices == null)
                                meshes["a"].Indices = new List<uint>();

                            //f <int|vertex/uv/normal vertex/uv/normal vertex/uv/normal>
                            var x = Convert.ToUInt32(split[1].Split('/')[0]);
                            meshes["a"].Indices.Add(x);
                            var y = Convert.ToUInt32(split[2].Split('/')[0]);
                            meshes["a"].Indices.Add(y);
                            if (split.Length > 2)
                            {
                                var z = Convert.ToUInt32(split[3].Split('/')[0]);
                                meshes["a"].Indices.Add(z);
                            }
                        }
                    }
                }
            }

            return meshes.Values.ToArray();
        }
        
    }
}
