using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using Jellyfish.FileFormats.Models;
using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish;

public static class ModelParser
{
    public static MeshPart[] Parse(string path)
    {
        if (Path.GetExtension(path) == "mdl")
            return MDL.Load(path[..^4]).Vtx.MeshParts.ToArray();

        var importer = new AssimpContext();
        var scene = importer.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateUVCoords | PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.OptimizeMeshes | PostProcessSteps.OptimizeGraph);

        var mashParts = new List<MeshPart>();
        foreach (var mesh in scene.Meshes)
        {
            var coords = mesh.Vertices.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
            var uvs = mesh.TextureCoordinateChannels[0].Select(x => new Vector2(x.X, x.Y)).ToArray();
            var normals = mesh.Normals.Select(x=> new Vector3(x.X, x.Y, x.Z)).ToArray();

            var verticies = new List<Vertex>();
            for (var i = 0; i < coords.Length; i++)
            {
                verticies.Add(new Vertex
                {
                    Coordinates = coords[i],
                    Normal = normals[i],
                    UV = uvs[i]
                });
            }

            mashParts.Add(new MeshPart
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Vertices = verticies,
                Indices = mesh.GetUnsignedIndices().ToList(),
                Texture = scene.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath
            });
        }

        return mashParts.ToArray();
    }
}