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

    // probe closest to the scene center, blended in everywhere with a tiny weight so points outside of every radius still get lit
    public int FallbackProbe;

    // written by ProbeCulling.comp
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = ImageBasedLighting.cluster_count)]
    public uint[] ClusterProbeCount;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = ImageBasedLighting.cluster_count * ImageBasedLighting.max_probes_per_cluster)]
    public uint[] ClusterProbeIndices;
}

[StructLayout(LayoutKind.Sequential)]
public struct LightProbe : IGpuStruct
{
    public ulong PrefilterTexture;
    private ulong _pad;
    public Vector4 Position; // w = influence radius
    public Vector4 Sh0, Sh1, Sh2, Sh3, Sh4, Sh5, Sh6, Sh7, Sh8;
}