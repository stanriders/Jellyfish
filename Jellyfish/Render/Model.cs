using System.Collections.Generic;
using System.Linq;
using Jellyfish.Console;
using Jellyfish.Utils;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public struct Bone
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? Parent { get; set; } = null;
    public Matrix4 OffsetMatrix { get; set; }

    public override string ToString() => $"{Id} - {Name}";

    public Bone()
    {
    }
}

public class Model
{
    public string Name { get; set; }
    public List<AnimationClip> Animations { get; private set; } = new();
    public List<Bone> Bones { get; private set; } = new();
    public Matrix4[] BoneMatrices { get; private set; } = [];

    public ModelAnimator? Animator { get; }

    private readonly List<Mesh> _meshes = new();
    private bool _shouldDraw = true;
    public IReadOnlyList<Mesh> Meshes => _meshes.AsReadOnly();

    public bool ShouldDraw
    {
        get => _shouldDraw;
        set
        {
            foreach (var mesh in _meshes)
            {
                mesh.ShouldDraw = value;
            }
            _shouldDraw = value;
        }
    }

    public Model(string name, List<Mesh> meshes, List<Bone> bones, List<AnimationClip> animations, bool isDev = false)
    {
        Name = name;

        if (meshes.Count <= 0)
        {
            Log.Context(this).Error("Failed to create Model!");
            return;
        }

        foreach (var meshPart in meshes)
        {
            meshPart.Model = this;
            meshPart.IsDev = isDev;
            _meshes.Add(meshPart);
        }

        Animations = animations;
        Bones = bones;
        BoneMatrices = Enumerable.Repeat(Matrix4.Identity, Bones.Count).ToArray();

        foreach (var mesh in _meshes)
            Engine.MeshManager.AddMesh(mesh);

        Animator = new ModelAnimator(this);
    }

    public Model(string name, Mesh mesh, List<Bone> bones, bool isDev = false)
    {
        Name = name;
        Bones = bones;
        BoneMatrices = Enumerable.Repeat(Matrix4.Identity, Bones.Count).ToArray();

        mesh.IsDev = isDev;
        _meshes.Add(mesh);

        Engine.MeshManager.AddMesh(mesh);
    }

    public void Update(double frameTime)
    {
        Animator?.Update(frameTime);
        if (Animator?.CurrentClip != null)
            BoneMatrices = Animator.FinalBoneMatrices;
    }

    public void Unload()
    {
        foreach (var mesh in _meshes)
            Engine.MeshManager.RemoveMesh(mesh);
    }

    public Vector3 Position
    {
        get
        {
            if (_meshes.Count != 0)
                return _meshes[0].Position;

            return Vector3.Zero;
        }
        set
        {
            if (_meshes.Count != 0)
            {
                foreach (var mesh in _meshes)
                    mesh.Position = value;
            }
        }
    }

    public Quaternion Rotation
    {
        get
        {
            if (_meshes.Count != 0)
                return _meshes[0].Rotation;

            return Quaternion.Identity;
        }
        set
        {
            if (_meshes.Count != 0)
            {
                foreach (var mesh in _meshes)
                    mesh.Rotation = value;
            }
        }
    }

    public Vector3 Scale
    {
        get
        {
            if (_meshes.Count != 0)
                return _meshes[0].Scale;

            return Vector3.One;
        }
        set
        {
            if (_meshes.Count != 0)
            {
                foreach (var mesh in _meshes)
                    mesh.Scale = value;
            }
        }
    }

    public BoundingBox BoundingBox
    {
        get
        {
            var bindPoseBoundingBox = new BoundingBox(_meshes.Select(x => x.BoundingBox).ToArray());

            // at least one bone since we can't build a box using one point
            if (Bones.Count > 1)
            {
                var modelBoundingBox = new BoundingBox(Bones.ToArray(), Animator?.UnoffsetBoneMatrices ?? BoneMatrices);
                if (modelBoundingBox.Size.Length > 0)
                {
                    return modelBoundingBox.Translate(Matrix4.Identity * 
                                                      Matrix4.CreateScale(Scale) *
                                                      Matrix4.CreateFromQuaternion(Rotation));
                }
            }
            return bindPoseBoundingBox;
        }
    }
}