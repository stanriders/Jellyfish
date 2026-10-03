using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<string, Scene> _meshesCache = new ConcurrentDictionary<string, Scene>();

    public static Model? Parse(string path, bool isDev = false)
    {
        Log.Context("ModelParser").Information("Loading model {Path}...", path);

        if (!Path.Exists(path))
        {
            path = $"models/{path}";

            if (!Path.Exists(path))
            {
                Log.Context("ModelParser").Error("Can't find model {Model}", path);
                return null;
            }
        }

        var modelName = Path.GetFileNameWithoutExtension(path);

        if (Path.GetExtension(path) == ".mdl")
            return new Model(modelName, MDL.Load(path[..^4]).Vtx.Meshes, [], [], isDev);

        using var importer = new AssimpContext();

        if (!_meshesCache.TryGetValue(path, out var scene))
        {
            scene = importer.ImportFile(path, PostProcessSteps.Triangulate |
                                                  PostProcessSteps.GenerateUVCoords |
                                                  PostProcessSteps.JoinIdenticalVertices |
                                                  PostProcessSteps.OptimizeMeshes |
                                                  PostProcessSteps.OptimizeGraph |
                                                  PostProcessSteps.SortByPrimitiveType |
                                                  PostProcessSteps.ImproveCacheLocality);
            _meshesCache.TryAdd(path, scene);
        }

        var isSmd = Path.GetExtension(path) == ".smd";
        var prerotate = isSmd;

        var meshes = new List<Mesh>();

        var bones = new List<Bone>();
        var boneMap = new Dictionary<string, int>();

        // collect node transforms of every mesh
        var meshTransforms = new Dictionary<int, List<Matrix4>>();
        var nodes = new Stack<(Node Node, Matrix4 ParentTransform)>();
        nodes.Push((scene.RootNode, Matrix4.Identity));
        while (nodes.TryPop(out var node))
        {
            var nodeTransform = ((Matrix4)node.Node.Transform).Transposed() * node.ParentTransform;

            foreach (var meshIndex in node.Node.MeshIndices)
            {
                if (!meshTransforms.TryGetValue(meshIndex, out var transforms))
                    meshTransforms[meshIndex] = transforms = [];

                transforms.Add(nodeTransform);
            }

            foreach (var child in node.Node.Children)
                nodes.Push((child, nodeTransform));
        }

        // skinned meshes are placed by their bones, everything else gets its node transform baked in.
        // a mesh can be referenced by several nodes, in which case each reference becomes its own mesh
        var meshInstances = scene.Meshes.SelectMany((mesh, i) =>
            !prerotate && !mesh.HasBones && meshTransforms.TryGetValue(i, out var transforms)
                ? transforms.Select(transform => (mesh, transform))
                : new[] { (mesh, Matrix4.Identity) });

        foreach (var (mesh, transform) in meshInstances)
        {
            var coords = mesh.Vertices.Select(x => new Vector3(x.X, x.Y, x.Z)).ToArray();
            var uvs = mesh.TextureCoordinateChannels[0].Select(x => new Vector2(x.X, x.Y)).ToArray();
            var normals = mesh.Normals.Select(x=> new Vector3(x.X, x.Y, x.Z)).ToArray();
            var indices = mesh.GetUnsignedIndices().ToList();

            if (prerotate)
            {
                coords = coords.Select(x => Vector3.Transform(x, new Quaternion(MathHelper.DegreesToRadians(-90), 0, 0))).ToArray();
                normals = normals.Select(x => Vector3.Transform(x, new Quaternion(MathHelper.DegreesToRadians(-90), 0, 0))).ToArray();
            }

            if (transform != Matrix4.Identity)
            {
                coords = coords.Select(x => Vector3.TransformPosition(x, transform)).ToArray();
                normals = normals.Select(x => Vector3.TransformNormal(x, transform).Normalized()).ToArray();

                // mirroring transforms flip the winding order
                if (transform.Determinant < 0)
                {
                    for (var i = 0; i + 2 < indices.Count; i += 3)
                        (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                }
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

                    var offsetMatrix = ((Matrix4)bone.OffsetMatrix).Transposed();
                    if (prerotate)
                    {
                        var corr = Matrix4.CreateFromQuaternion(new Quaternion(MathHelper.DegreesToRadians(90), 0, 0));
                        offsetMatrix = corr * offsetMatrix;
                    }

                    bones.Add(new Bone
                    {
                        Id = boneId,
                        Name = bone.Name,
                        OffsetMatrix = offsetMatrix
                    });
                }

                foreach (var vertexWeight in bone.VertexWeights)
                {
                    if (vertexWeight.Weight > 0)
                    {
                        verticies[vertexWeight.VertexID].BoneLinks.Add(new BoneLink
                        {
                            Id = boneMap[bone.Name],
                            Weigth = vertexWeight.Weight
                        });
                    }
                }
            }

            var texturePath = scene.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath ?? scene.Materials[mesh.MaterialIndex].Name;

            meshes.Add(new Mesh($"{modelName}_{meshes.Count}", 
                verticies,
                indices,
                texturePath));
        }

        BuildBoneHierarchy(scene.RootNode, null, boneMap, bones, prerotate);

        var animations = new List<AnimationClip>();
        if (isSmd)
        {
            // custom loader for smd anims since they're stored as separate files
            var animationFiles = Directory.EnumerateFiles(Path.GetDirectoryName(path)!, $"{modelName}__*.smd").ToArray();
            foreach (var animationFile in animationFiles)
            {
                var animationScene = importer.ImportFile(animationFile);
                var animationName = Path.GetFileNameWithoutExtension(animationFile).Replace($"{modelName}__", "");

                animations.AddRange(LoadAnimations(animationScene.Animations, bones, prerotate, animationName));
            }
        }
        else
        {
            animations = LoadAnimations(scene.Animations, bones);
        }

        return new Model(modelName, meshes, bones, animations, isDev);
    }

    private static void BuildBoneHierarchy(Node node, int? parentIndex, Dictionary<string, int> boneMap, List<Bone> bones, bool prerotate)
    {
        if (boneMap.TryGetValue(node.Name, out int boneIndex))
        {
            var bone = bones[boneIndex];
            bone.Parent = parentIndex;

            bones[boneIndex] = bone; // reassign because Bone is a struct
            parentIndex = boneIndex; // now children see this as parent
        }

        foreach (var child in node.Children)
            BuildBoneHierarchy(child, parentIndex, boneMap, bones, prerotate);
    }

    private static List<AnimationClip> LoadAnimations(List<Animation> assimpAnimations, List<Bone> bones, bool prerotate = false, string? name = null)
    {
        var animations = new List<AnimationClip>();
        foreach (var anim in assimpAnimations)
        {
            var ticksPerSecond = anim.TicksPerSecond != 0 ? anim.TicksPerSecond : 25.0;

            var clip = new AnimationClip
            {
                Name = string.IsNullOrEmpty(anim.Name) ? (name ?? assimpAnimations.IndexOf(anim).ToString()) : anim.Name,
                Duration = anim.DurationInTicks / ticksPerSecond
            };

            foreach (var channel in anim.NodeAnimationChannels)
            {
                if (!bones.Any(b => b.Name == channel.NodeName))
                    continue; // skip channels not affecting this model

                var boneAnim = new BoneAnimation { BoneName = channel.NodeName };

                foreach (var pos in channel.PositionKeys)
                {
                    var position = new Vector3(pos.Value.X, pos.Value.Y, pos.Value.Z);
                    boneAnim.PositionKeys.Add(new Keyframe<Vector3>(pos.Time / ticksPerSecond, position));
                }

                foreach (var rot in channel.RotationKeys)
                {
                    var rotation = new Quaternion(rot.Value.X, rot.Value.Y, rot.Value.Z, rot.Value.W);
                    boneAnim.RotationKeys.Add(new Keyframe<Quaternion>(rot.Time / ticksPerSecond, rotation));
                }

                foreach (var sca in channel.ScalingKeys)
                {
                    var scale = new Vector3(sca.Value.X, sca.Value.Y, sca.Value.Z);
                    boneAnim.ScalingKeys.Add(new Keyframe<Vector3>(sca.Time / ticksPerSecond, scale));
                }

                clip.BoneAnimations.Add(boneAnim);
            }

            if (clip.BoneAnimations.Count > 0)
                animations.Add(clip);
        }

        return animations;
    }
}