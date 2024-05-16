using Jellyfish.Render;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTK.Mathematics;

namespace Jellyfish.FileFormats.Models;

public static class GLTF
{
    public static MeshPart[] LoadGLB(string path)
    {
        return ConvertGLTF(ModelRoot.ReadGLB(File.OpenRead(path), new ReadSettings
        {
            Validation = ValidationMode.TryFix
        }), Path.GetFileNameWithoutExtension(path));
    }

    public static MeshPart[] LoadGLTF(string path)
    {
        return ConvertGLTF(ModelRoot.Load(path, ReadContext.Create(assetFileName =>
        {
            var assetPath = File.Exists(assetFileName)
                ? assetFileName
                : Path.Combine(Path.GetDirectoryName(path)!, assetFileName);

            // asset isn't found near the gltf file, try looking into materials
            if (!File.Exists(assetPath))
            {
                assetPath = $"materials/models/{Path.GetFileNameWithoutExtension(path)}/{assetFileName}";
            }

            return File.Exists(assetPath)
                ? new ArraySegment<byte>(File.ReadAllBytes(assetPath))
                : new ArraySegment<byte>(Array.Empty<byte>());
        })), Path.GetFileNameWithoutExtension(path));
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
                    Name = name, // FIXME?
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
                    meshPart.Texture = primitive.Material.GetDiffuseTexture().PrimaryImage.Content.SourcePath;

                meshPart.Indices = primitive.GetIndices().ToList();
                meshes.Add(meshPart);
            }
        }

        return meshes.ToArray();
    }
}