using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;
using System.Runtime.InteropServices;

namespace Jellyfish.Render.Shaders.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct LightProbes : IGpuStruct
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = ImageBasedLighting.max_probes)]
    public LightProbe[] Probes;

    public int ProbeCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct LightProbe : IGpuStruct
{
    public ulong PrefilterTexture;
    private ulong _pad;
    public Vector4 Position; // w = influence radius
    public Vector4 Sh0, Sh1, Sh2, Sh3, Sh4, Sh5, Sh6, Sh7, Sh8;
}