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
    public ulong IrradianceTexture;
    public ulong PrefilterTexture;
    public Vector4 Position;
}