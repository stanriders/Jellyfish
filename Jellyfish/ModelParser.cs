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
    public static MeshInfo[]? Parse(string path)
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
                return MDL.Load(path[..^4]).Vtx.MeshInfos.ToArray();
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
            var shouldParse = false;
            var currentTexture = string.Empty;
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

                    if (line == "end" && shouldParse)
                    {
                        shouldParse = false;
                        continue;
                    }

                    if (shouldParse)
                    {
                        var split = line.Trim().Split(' ').Where(x => !string.IsNullOrEmpty(x)).ToArray();
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
                                meshes.Add(line, new MeshInfo
                                {
                                    Name = Path.GetFileNameWithoutExtension(path),
                                    Texture = line
                                });

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
        var mesh = new MeshInfo
        {
            Name = Path.GetFileNameWithoutExtension(path),
            Texture = "eye.jpg"
        };

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

                        mesh.Vertices.Add(vertex);
                    }

                    if (split[0] == "vt")
                    {
                        //vt <float|PosX PosY PosZ>
                        var uv = new Vector2(Convert.ToSingle(split[1], CultureInfo.InvariantCulture),
                            Convert.ToSingle(split[2], CultureInfo.InvariantCulture));

                        mesh.UVs.Add(uv);
                    }
                    else if (split[0] == "vn")
                    {
                        //vn <float|PosX PosY PosZ>
                        var normal = new Vector3(Convert.ToSingle(split[1], CultureInfo.InvariantCulture),
                            Convert.ToSingle(split[2], CultureInfo.InvariantCulture),
                            Convert.ToSingle(split[3], CultureInfo.InvariantCulture));

                        mesh.Normals.Add(normal);
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

    private static MeshInfo[] ParseGLB(string path)
    {
        return ConvertGLTF(ModelRoot.ReadGLB(File.OpenRead(path), new ReadSettings
        {
            SkipValidation = true
        }));
    }

    private static MeshInfo[] ParseGLTF(string path)
    {
        return ConvertGLTF(ModelRoot.ParseGLTF(File.ReadAllText(path), new ReadSettings
        {
            FileReader = assetFileName =>
                new ArraySegment<byte>(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(path)!, assetFileName))),
            SkipValidation = true
        }));
    }

    private static MeshInfo[] ConvertGLTF(ModelRoot gltf)
    {
        var meshes = new List<MeshInfo>();

        //foreach (var mesh in gltf.LogicalMeshes)
        for (var i = 0; i < gltf.LogicalMeshes.Count; i++)
        {
            var mesh = gltf.LogicalMeshes[i];
            var meshInfo = new MeshInfo()
            {
                Name = "gltf" // FIXME!
            };
            foreach (var primitive in mesh.Primitives)
            {
                meshInfo.Vertices = primitive.GetVertices("POSITION")
                    .AsVector3Array()
                    .Select(x => new Vector3(x.X, x.Y + i * 2, x.Z))
                    .ToList();

                if (primitive.VertexAccessors.ContainsKey("NORMAL"))
                    meshInfo.Normals = primitive.GetVertices("NORMAL")
                        .AsVector3Array()
                        .Select(x => new Vector3(x.X, x.Y, x.Z))
                        .ToList();

                if (primitive.VertexAccessors.ContainsKey("TEXCOORD_0"))
                    meshInfo.UVs = primitive.GetVertices("TEXCOORD_0")
                        .AsVector2Array()
                        .Select(x => new Vector2(x.X, x.Y))
                        .ToList();

                if (meshInfo.Texture == null)
                    meshInfo.Texture = primitive.Material.Name + "_baseColor.png";

                meshInfo.Indices = primitive.GetIndices().ToList();
            }


            meshes.Add(meshInfo);
        }

        return meshes.ToArray();
    }
}