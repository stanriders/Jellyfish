using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp;
using Jellyfish.FileFormats.Models;
using Jellyfish.Render;
using OpenTK.Mathematics;
using Mesh = Jellyfish.Render.Mesh;
using Quaternion = OpenTK.Mathematics.Quaternion;

namespace Jellyfish;

public static class ModelParser
{
    public static Mesh[] Parse(string path)
    {
        if (Path.GetExtension(path) == ".mdl")
            return MDL.Load(path[..^4]).Vtx.Meshes.ToArray();

        var importer = new AssimpContext();
        var scene = importer.ImportFile(path, PostProcessSteps.Triangulate | 
                                              PostProcessSteps.GenerateUVCoords | 
                                              PostProcessSteps.JoinIdenticalVertices | 
                                              PostProcessSteps.OptimizeMeshes | 
                                              PostProcessSteps.OptimizeGraph);

        var prerotate = Path.GetExtension(path) == ".smd";

        var mashes = new List<Mesh>();
        foreach (var mesh in scene.Meshes)
        {
            var coords = mesh.Vertices.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
            var uvs = mesh.TextureCoordinateChannels[0].Select(x => new Vector2(x.X, x.Y)).ToArray();
            var normals = mesh.Normals.Select(x=> new Vector3(x.X, x.Y, x.Z)).ToArray();

            if (prerotate)
            {
                coords = coords.Select(x => Vector3.Transform(x, new Quaternion(MathHelper.DegreesToRadians(-90), 0, 0))).ToArray();
                normals = normals.Select(x => Vector3.Transform(x, new Quaternion(MathHelper.DegreesToRadians(-90), 0, 0))).ToArray();
            }

            var verticies = new List<Vertex>();
            for (var i = 0; i < coords.Length; i++)
            {
                verticies.Add(new Vertex
                {
                    Coordinates = coords[i],
                    Normal = normals[i],
                    UV = uvs.Length == coords.Length ? uvs[i] : new Vector2()
                });
            }

            var bones = new List<Render.Bone>();
            for (var i = 0; i < mesh.Bones.Count; i++)
            {
                var bone = mesh.Bones[i];
                bones.Add(new Render.Bone { Id = i, Name = bone.Name });

                foreach (var vertexWeight in bone.VertexWeights)
                {
                    verticies[vertexWeight.VertexID].BoneLinks.Add(new BoneLink { Id = i, Weigth = vertexWeight.Weight});
                }
            }
            
            mashes.Add(new Mesh(Path.GetFileNameWithoutExtension(path), 
                verticies, 
                mesh.GetUnsignedIndices().ToList(), 
                bones, 
                scene.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath ?? scene.Materials[mesh.MaterialIndex].Name));
        }

        return mashes.ToArray();
    }
}