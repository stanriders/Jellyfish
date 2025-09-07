using Assimp;
using Jellyfish.Console;
using Jellyfish.FileFormats.Models;
using Jellyfish.Render;
using OpenTK.Mathematics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bone = Jellyfish.Render.Bone;
using Mesh = Jellyfish.Render.Mesh;
using Quaternion = OpenTK.Mathematics.Quaternion;

namespace Jellyfish;

public static class ModelParser
{
    public static Model Parse(string path, bool isDev = false)
    {
        Log.Context("ModelParser").Information("Loading model {Path}...", path);

        var modelName = Path.GetFileNameWithoutExtension(path);

        if (Path.GetExtension(path) == ".mdl")
            return new Model(modelName, MDL.Load(path[..^4]).Vtx.Meshes, [], [], isDev);

        var importer = new AssimpContext();
        var scene = importer.ImportFile(path, PostProcessSteps.Triangulate | 
                                              PostProcessSteps.GenerateUVCoords | 
                                              PostProcessSteps.JoinIdenticalVertices | 
                                              PostProcessSteps.OptimizeMeshes | 
                                              PostProcessSteps.OptimizeGraph | 
                                              PostProcessSteps.SplitLargeMeshes | 
                                              PostProcessSteps.SortByPrimitiveType);

        var isSmd = Path.GetExtension(path) == ".smd";
        var prerotate = isSmd;

        var meshes = new List<Mesh>();

        var bones = new List<Bone>();
        var boneMap = new Dictionary<string, int>();

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

            foreach (var bone in mesh.Bones)
            {
                if (!boneMap.TryGetValue(bone.Name, out var boneId))
                {
                    boneId = bones.Count;
                    boneMap[bone.Name] = boneId;

                    bones.Add(new Bone
                    {
                        Id = boneId,
                        Name = bone.Name,
                        OffsetMatrix = bone.OffsetMatrix.ToOpentkMatrix()
                    });
                }

                foreach (var vertexWeight in bone.VertexWeights)
                {
                    verticies[vertexWeight.VertexID].BoneLinks.Add(new BoneLink
                    {
                        Id = boneMap[bone.Name],
                        Weigth = vertexWeight.Weight
                    });
                }
            }

            var texturePath = scene.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath ?? scene.Materials[mesh.MaterialIndex].Name;

            meshes.Add(new Mesh($"{modelName}_{meshes.Count}", 
                verticies, 
                mesh.GetUnsignedIndices().ToList(), 
                texturePath));
        }

        BuildBoneHierarchy(scene.RootNode, null, boneMap, bones);

        var animations = new List<AnimationClip>();
        if (isSmd)
        {
            var animationFiles = Directory.EnumerateFiles(Path.GetDirectoryName(path)!, $"{modelName}__*.smd").ToArray();
            foreach (var animationFile in animationFiles)
            {
                var animationScene = importer.ImportFile(animationFile);
                animations = LoadAnimations(animationScene.Animations, bones);
            }
        }
        else
        {
            animations = LoadAnimations(scene.Animations, bones);
        }

        return new Model(modelName, meshes, bones, animations, isDev);
    }

    private static void BuildBoneHierarchy(Node node, int? parentIndex, Dictionary<string, int> boneMap, List<Bone> bones)
    {
        if (boneMap.TryGetValue(node.Name, out int boneIndex))
        {
            var bone = bones[boneIndex];
            bone.Parent = parentIndex;
            bones[boneIndex] = bone; // reassign because Bone is a struct
            parentIndex = boneIndex; // now children see this as parent
        }

        foreach (var child in node.Children)
            BuildBoneHierarchy(child, parentIndex, boneMap, bones);
    }

    private static List<AnimationClip> LoadAnimations(List<Animation> assimpAnimations, List<Bone> bones)
    {
        var animations = new List<AnimationClip>();
        foreach (var anim in assimpAnimations)
        {
            var clip = new AnimationClip
            {
                Name = anim.Name,
                Duration = anim.DurationInTicks / (anim.TicksPerSecond != 0 ? anim.TicksPerSecond : 25.0)
            };

            foreach (var channel in anim.NodeAnimationChannels)
            {
                if (!bones.Any(b => b.Name == channel.NodeName))
                    continue; // skip channels not affecting this model

                var boneAnim = new BoneAnimation { BoneName = channel.NodeName };

                foreach (var pos in channel.PositionKeys)
                    boneAnim.PositionKeys.Add(new Keyframe<Vector3>(
                        pos.Time / (anim.TicksPerSecond != 0 ? anim.TicksPerSecond : 25.0),
                        new Vector3(pos.Value.X, pos.Value.Y, pos.Value.Z)));

                foreach (var rot in channel.RotationKeys)
                    boneAnim.RotationKeys.Add(new Keyframe<Quaternion>(
                        rot.Time / (anim.TicksPerSecond != 0 ? anim.TicksPerSecond : 25.0),
                        new Quaternion((float)rot.Value.X, (float)rot.Value.Y, (float)rot.Value.Z,
                            (float)rot.Value.W)));

                foreach (var sca in channel.ScalingKeys)
                    boneAnim.ScalingKeys.Add(new Keyframe<Vector3>(
                        sca.Time / (anim.TicksPerSecond != 0 ? anim.TicksPerSecond : 25.0),
                        new Vector3(sca.Value.X, sca.Value.Y, sca.Value.Z)));

                clip.BoneAnimations.Add(boneAnim);
            }

            if (clip.BoneAnimations.Count > 0)
                animations.Add(clip);
        }

        return animations;
    }
}