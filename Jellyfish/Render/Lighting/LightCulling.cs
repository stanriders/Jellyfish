using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Lighting;

public class LightCulling
{
    private const int ClustersX = 16;
    private const int ClustersY = 9;
    private const int ClustersZ = 24;
    private const int MaxLightsPerCluster = 64;
    private const int ClusterCount = ClustersX * ClustersY * ClustersZ;

    public readonly ShaderStorageBuffer ClustersBuffer = new("lightClustersSSBO", (ClusterCount + ClusterCount * MaxLightsPerCluster) * sizeof(uint));
    private readonly CullingShader _shader = new();

    public void Update()
    {
        using var _ = new PerformanceMeasure("LightClusters.Update");

        var viewport = Engine.MainViewport;

        Engine.LightManager.LightSourcesSsbo.Bind(0);
        ClustersBuffer.Bind(2);

        _shader.Bind();
        _shader.SetMatrix4("view", viewport.GetViewMatrix());
        _shader.SetMatrix4("invProjection", viewport.GetProjectionMatrix().Inverted());
        _shader.SetFloat("near", viewport.NearPlane);
        _shader.SetFloat("far", viewport.FarPlane);
        _shader.DispatchCompute(1, 1, ClustersZ, MemoryBarrierMask.ShaderStorageBarrierBit);
        _shader.Unbind();
    }

    public void Unload()
    {
        _shader.Unload();
        ClustersBuffer.Dispose();
    }

    private class CullingShader() : Shader("shaders/LightCulling.comp");
}