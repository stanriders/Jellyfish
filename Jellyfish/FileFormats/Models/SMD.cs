using Jellyfish.Render;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenTK.Mathematics;

namespace Jellyfish.FileFormats.Models;

public static class SMD
{
    public static MeshPart[] Load(string path)
    {
        var meshes = new Dictionary<string, MeshPart>();
        var bones = new List<Bone>();

        var file = File.ReadAllText(path);
        using (var reader = new StringReader(file))
        {
            var parsingBones = false;
            var parsingTriangles = false;
            var currentTexture = string.Empty;
            while (reader.Peek() != -1)
            {
                var line = reader.ReadLine();
                if (line != null)
                {
                    if (line == "triangles" && !parsingTriangles)
                    {
                        parsingTriangles = true;
                        continue;
                    }

                    if (line == "nodes" && !parsingBones)
                    {
                        parsingBones = true;
                        continue;
                    }

                    if (line == "end")
                    {
                        if (parsingTriangles)
                            parsingTriangles = false;

                        if (parsingBones)
                            parsingBones = false;

                        continue;
                    }

                    if (parsingBones)
                    {
                        //<int|ID> "<string|Bone Name>" <int|Parent ID>
                        var split = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();
                        var bone = new Bone
                        {
                            Id = Convert.ToInt32(split[0]),
                            Name = split[1],
                            Parent = split.Length > 2 ? Convert.ToInt32(split[2]) : null,
                        };

                        bones.Add(bone);
                    }

                    if (parsingTriangles)
                    {
                        var data = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToArray();
                        if (int.TryParse(data[0], out var parentBone))
                        {
                            //<int|Parent bone> <float|PosX PosY PosZ> <normal|NormX NormY NormZ> <normal|U V> <int|links> <int|Bone ID> <normal|Weight> [...]
                            var coords = new Vector3(Convert.ToSingle(data[1], CultureInfo.InvariantCulture),
                                Convert.ToSingle(data[2], CultureInfo.InvariantCulture),
                                Convert.ToSingle(data[3], CultureInfo.InvariantCulture));

                            var normal = new Vector3(Convert.ToSingle(data[4], CultureInfo.InvariantCulture),
                                Convert.ToSingle(data[5], CultureInfo.InvariantCulture),
                                Convert.ToSingle(data[6], CultureInfo.InvariantCulture));

                            var uv = new Vector2(Convert.ToSingle(data[7], CultureInfo.InvariantCulture),
                                Convert.ToSingle(data[8], CultureInfo.InvariantCulture));

                            var vertex = new Vertex
                            {
                                Coordinates = coords,
                                Normal = normal,
                                UV = uv
                            };

                            var boneLinks = Convert.ToInt32(data[9]);
                            if (boneLinks > 0)
                            {
                                for (var i = 0; i < boneLinks * 2; i += 2)
                                {
                                    var link = new BoneLink
                                    {
                                        Id = Convert.ToInt32(data[10 + i]),
                                        Weigth = Convert.ToSingle(data[10 + i + 1])
                                    };
                                    vertex.BoneLinks.Add(link);
                                }
                            }
                            else
                            {
                                vertex.BoneLinks.Add(new BoneLink { Id = parentBone, Weigth = 1f });
                            }

                            meshes[currentTexture].Vertices.Add(vertex);
                        }
                        else
                        {
                            // we split model by textures 
                            if (!meshes.ContainsKey(line))
                                meshes.Add(line, new MeshPart
                                {
                                    Name = Path.GetFileNameWithoutExtension(path),
                                    Texture = line,
                                    Bones = bones
                                });

                            currentTexture = line;
                        }
                    }
                }
            }
        }

        return meshes.Values.ToArray();
    }
}