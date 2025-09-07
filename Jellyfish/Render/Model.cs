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

    public override string ToString() => $"{Id} - {Name}";

    public Bone()
    {
    }
}

public struct Keyframe<T>
{
    public double Time;   // in seconds
    public T Value;

    public Keyframe(double time, T value)
    {
        Time = time;
        Value = value;
    }
}

public class BoneAnimation
{
    public string BoneName { get; set; } = null!;

    public List<Keyframe<Vector3>> PositionKeys { get; set; } = new();
    public List<Keyframe<Quaternion>> RotationKeys { get; set; } = new();
    public List<Keyframe<Vector3>> ScalingKeys { get; set; } = new();
}

public class AnimationClip
{
    public string Name { get; set; } = null!;
    public double Duration { get; set; } // seconds
    public List<BoneAnimation> BoneAnimations { get; set; } = new();
}

public class Model
{
    public string Name { get; set; }
    public List<AnimationClip> Animations { get; private set; } = new();
    public List<Bone> Bones { get; private set; } = new();

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

        foreach (var mesh in _meshes)
            Engine.MeshManager.AddMesh(mesh);
    }

    public Model(string name, Mesh mesh, List<Bone> bones, bool isDev = false)
    {
        Name = name;
        Bones = bones;

        mesh.IsDev = isDev;
        _meshes.Add(mesh);

        Engine.MeshManager.AddMesh(mesh);
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

    public BoundingBox BoundingBox => new BoundingBox(_meshes.Select(x => x.BoundingBox).ToArray());
}