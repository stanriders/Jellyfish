using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace Jellyfish.Render.Shaders.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct Light : IGpuStruct
{
    public Vector4 Position;
    public Vector4 Direction;
    public Matrix4 LightSpaceMatrix;

    public int Type;
    public float Constant;
    public float Linear;
    public float Quadratic;

    public float Cone;
    public float Outcone;
    public float Brightness;
    public bool HasShadows;

    public Vector4 Ambient;
    public Vector4 Diffuse;

    public float Near;
    public float Far;
    public bool UsePcss;
    private float _pad;
}

[StructLayout(LayoutKind.Sequential)]
public struct Sun : IGpuStruct
{
    public Vector4 Direction;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Entities.Sun.cascades)]
    public Matrix4[] LightSpaceMatrix;

    public Vector4 Ambient;
    public Vector4 Diffuse;

    public float Brightness;
    public int HasShadows;
    public int UsePcss;
    private int _pad3;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Entities.Sun.cascades)]
    public int[] CascadeFar;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = Entities.Sun.cascades)]
    public int[] CascadeNear;
}

[StructLayout(LayoutKind.Sequential)]
public struct LightSources : IGpuStruct
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = LightManager.max_lights)]
    public Light[] Lights;

    public Sun Sun;
}