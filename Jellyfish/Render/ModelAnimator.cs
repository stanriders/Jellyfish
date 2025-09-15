using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Render;

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

public class ModelAnimator
{
    private readonly Model _model;

    public AnimationClip? CurrentClip { get; private set; }
    public double Time { get; private set; }
    public Matrix4[] FinalBoneMatrices { get; }
    public Matrix4[] UnoffsetBoneMatrices { get; }

    public ModelAnimator(Model model)
    {
        _model = model;

        FinalBoneMatrices = Enumerable.Repeat(Matrix4.Identity, model.Bones.Count).ToArray();
        UnoffsetBoneMatrices = Enumerable.Repeat(Matrix4.Identity, model.Bones.Count).ToArray();
    }

    public void Play(AnimationClip clip)
    {
        CurrentClip = clip;
        Time = 0;
    }

    public void Update(double deltaTime)
    {
        if (CurrentClip == null || _model.Bones.Count == 0)
            return;

        Time = (Time + deltaTime) % CurrentClip.Duration;

        // start from skeleton roots (bones without parent)
        for (int i = 0; i < _model.Bones.Count; i++)
        {
            if (_model.Bones[i].Parent == null)
            {
                TraverseBoneHierarchy(i, Matrix4.Identity);
            }
        }
    }

    private void TraverseBoneHierarchy(int boneIndex, Matrix4 parentTransform)
    {
        var bone = _model.Bones[boneIndex];

        // build local transform
        Matrix4 localTransform = Matrix4.Identity;

        var boneAnim = CurrentClip!.BoneAnimations
            .FirstOrDefault(b => b.BoneName == bone.Name);

        if (boneAnim != null)
        {
            var pos = Interpolate(boneAnim.PositionKeys, Time, Vector3.Zero);
            var rot = Interpolate(boneAnim.RotationKeys, Time, Quaternion.Identity);
            var sca = Interpolate(boneAnim.ScalingKeys, Time, Vector3.One);

            localTransform =
                Matrix4.CreateScale(sca) *
                Matrix4.CreateFromQuaternion(rot) *
                Matrix4.CreateTranslation(pos);
        }

        var globalTransform = localTransform * parentTransform;
        FinalBoneMatrices[bone.Id] = bone.OffsetMatrix * globalTransform;
        UnoffsetBoneMatrices[bone.Id] = globalTransform;

        // recurse into children
        foreach (var child in _model.Bones.Where(b => b.Parent == bone.Id))
        {
            TraverseBoneHierarchy(child.Id, globalTransform);
        }
    }

    private static Vector3 Interpolate(List<Keyframe<Vector3>> keys, double t, Vector3 defaultValue)
    {
        if (keys.Count == 0) return defaultValue;
        if (keys.Count == 1) return keys[0].Value;

        int i = 0;
        while (i < keys.Count - 1 && t > keys[i + 1].Time) i++;

        int next = Math.Min(i + 1, keys.Count - 1);

        double span = keys[next].Time - keys[i].Time;
        double factor = span > 0 ? (t - keys[i].Time) / span : 0;

        return Vector3.Lerp(keys[i].Value, keys[next].Value, (float)factor);
    }

    private static Quaternion Interpolate(List<Keyframe<Quaternion>> keys, double t, Quaternion defaultValue)
    {
        if (keys.Count == 0) return defaultValue;
        if (keys.Count == 1) return keys[0].Value;

        int i = 0;
        while (i < keys.Count - 1 && t > keys[i + 1].Time) i++;

        int next = Math.Min(i + 1, keys.Count - 1);

        double span = keys[next].Time - keys[i].Time;
        double factor = span > 0 ? (t - keys[i].Time) / span : 0;

        return Quaternion.Slerp(keys[i].Value, keys[next].Value, (float)factor);
    }
}