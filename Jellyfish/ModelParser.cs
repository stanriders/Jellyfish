using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfish.FileFormats;
using Jellyfish.Render;
using OpenTK.Mathematics;
using SharpGLTF.Schema2;

namespace Jellyfish;

public static class ModelParser
{
    public static MeshPart[]? Parse(string path)
    {
        switch (Path.GetExtension(path))
        {
            case ".smd":
                return ParseSMD(path);
            case ".obj":
                return ParseOBJ(path);
            case ".gltf":
                return ParseGLTF(path);
            case ".glb":
                return ParseGLB(path);
            case ".mdl":
                return MDL.Load(path[..^4]).Vtx.MeshParts.ToArray();
            default:
                return null;
        }
    }

    private static MeshPart[] ParseSMD(string path)
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
                                vertex.BoneLinks.Add(new BoneLink {Id = parentBone, Weigth = 1f});
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

    private static MeshPart[] ParseOBJ(string path)
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

    private static MeshPart[] ParseGLB(string path)
    {
        return ConvertGLTF(ModelRoot.ReadGLB(File.OpenRead(path), new ReadSettings
        {
            SkipValidation = true
        }), Path.GetFileNameWithoutExtension(path));
    }

    private static MeshPart[] ParseGLTF(string path)
    {
        return ConvertGLTF(ModelRoot.ParseGLTF(File.ReadAllText(path), new ReadSettings
        {
            FileReader = assetFileName =>
            {
                var assetPath = Path.Combine(Path.GetDirectoryName(path)!, assetFileName);
                return File.Exists(assetPath) ? new ArraySegment<byte>(File.ReadAllBytes(assetPath)) : new ArraySegment<byte>();
            },
            SkipValidation = true
        }), Path.GetFileNameWithoutExtension(path));
    }

    private static MeshPart[] ConvertGLTF(ModelRoot gltf, string name)
    {
        var meshes = new List<MeshPart>();
        
        for (var i = 0; i < gltf.LogicalMeshes.Count; i++)
        {
            var mesh = gltf.LogicalMeshes[i];

            for (var j = 0; j < mesh.Primitives.Count; j++)
            {
                var primitive = mesh.Primitives[j];
                var meshPart = new MeshPart
                {
                    Name = string.IsNullOrEmpty(gltf.Asset.Copyright) ? name + i + j : gltf.Asset.Copyright, // FIXME!
                    Indices = new List<uint>()
                };

                var positions = primitive.GetVertices("POSITION")
                    .AsVector3Array()
                    .Select(x => new Vector3(x.X, x.Y, x.Z))
                    .ToList();

                var normals = new List<Vector3>();

                if (primitive.VertexAccessors.ContainsKey("NORMAL"))
                {
                    normals = primitive.GetVertices("NORMAL")
                        .AsVector3Array()
                        .Select(x => new Vector3(x.X, x.Y, x.Z))
                        .ToList();
                }

                var uvs = new List<Vector2>();

                if (primitive.VertexAccessors.ContainsKey("TEXCOORD_0"))
                {
                    uvs = primitive.GetVertices("TEXCOORD_0")
                        .AsVector2Array()
                        .Select(x => new Vector2(x.X, x.Y))
                        .ToList();
                }

                for (var k = 0; k < positions.Count; k++)
                {
                    meshPart.Vertices.Add(new Vertex
                    {
                        Coordinates = positions[k],
                        Normal = normals[k],
                        UV = uvs[k]
                    });
                }
                
                if (meshPart.Texture == null)
                    meshPart.Texture = name + "_baseColor.png";

                meshPart.Indices = primitive.GetIndices().ToList();
                meshes.Add(meshPart);
            }
        }

        return meshes.ToArray();
    }
}