using System;
using Jellyfish.Console;
using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders.Structs;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Render.Lighting;

public class IblEnabled() : ConVar<bool>("mat_ibl_enabled", true);
public class IblPrefilter() : ConVar<bool>("mat_ibl_prefilter", true);

public class ImageBasedLighting
{
    private class CullingShader() : Shader("shaders/ProbeCulling.comp");

    public List<LightProbe> Probes { get; } = new();
    private int _probesCount;

    public readonly ShaderStorageBuffer<LightProbes> LightProbesSsbo = new("lightProbesSSBO", new LightProbes());
    public const int max_probes = 512;

    private const int ClustersX = 16;
    private const int ClustersY = 9;
    private const int ClustersZ = 24;
    public const int cluster_count = ClustersX * ClustersY * ClustersZ;
    public const int max_probes_per_cluster = 32; // must match LightingDefinitions.shared

    private readonly CullingShader _cullingShader = new();

    public LightProbe? AddProbe(Vector3 position)
    {
        if (Probes.Count >= max_probes)
            return null;

        var probe = new LightProbe(_probesCount)
        {
            Position = position
        };
        Probes.Add(probe);
        _probesCount++;

        return probe;
    }

    public void RemoveProbe(LightProbe probe)
    {
        probe.Unload();
        Probes.Remove(probe);
    }

    public void PreFrame()
    {
        UpdateClusterBuffer();
    }

    public void Render(Sky? sky)
    {
        if (Probes.Count == 0)
        {
            UpdateProbeBuffer();
            return;
        }

        var iblState = ConVarStorage.Get<bool>("mat_ibl_enabled");
        var sslrState = ConVarStorage.Get<bool>("mat_sslr_enabled");

        ConVarStorage.Set("mat_ibl_enabled", false);
        ConVarStorage.Set("mat_sslr_enabled", false);

        UpdateProbeRadii();

        foreach (var lightProbe in Probes)
        {
            lightProbe.Render(sky);
        }

        ConVarStorage.Set("mat_ibl_enabled", iblState);
        ConVarStorage.Set("mat_sslr_enabled", sslrState);
        
        Engine.MainViewport.ViewMatrixOverride = null;
        Engine.MainViewport.ProjectionMatrixOverride = null;
        Engine.MainViewport.SizeOverride = null;

        UpdateProbeBuffer();

        // second pass to simulate one bounce of lighting
        foreach (var lightProbe in Probes)
        {
            lightProbe.Render(sky);
        }

        // SH lives in the buffer (unlike textures that got updated in place), so upload the bounced result too
        UpdateProbeBuffer();

        Engine.MainViewport.ViewMatrixOverride = null;
        Engine.MainViewport.ProjectionMatrixOverride = null;
        Engine.MainViewport.SizeOverride = null;
    }

    private void UpdateProbeRadii()
    {
        var distances = new List<float>(Probes.Count);

        foreach (var probe in Probes)
        {
            distances.Clear();
            foreach (var other in Probes)
            {
                if (other != probe)
                    distances.Add((other.Position - probe.Position).Length);
            }

            if (distances.Count == 0)
            {
                probe.Radius = 100_000f; // lone probe should light everything
                continue;
            }

            distances.Sort();

            var neighbourDistance = distances[Math.Min(4, distances.Count) - 1];
            probe.Radius = Math.Max(neighbourDistance, 0.1f);
        }
    }

    private void UpdateProbeBuffer()
    {
        var gpuProbes = ArrayPool<Jellyfish.Render.Shaders.Structs.LightProbe>.Shared.Rent(max_probes);

        for (var i = 0; i < Probes.Count; i++)
        {
            var probe = Probes[i];

            gpuProbes[i].Position = new Vector4(probe.Position, probe.Radius);
            gpuProbes[i].PrefilterTexture = probe.PrefilterBindlessHandle;
            gpuProbes[i].Sh0 = probe.IrradianceHarmonics[0];
            gpuProbes[i].Sh1 = probe.IrradianceHarmonics[1];
            gpuProbes[i].Sh2 = probe.IrradianceHarmonics[2];
            gpuProbes[i].Sh3 = probe.IrradianceHarmonics[3];
            gpuProbes[i].Sh4 = probe.IrradianceHarmonics[4];
            gpuProbes[i].Sh5 = probe.IrradianceHarmonics[5];
            gpuProbes[i].Sh6 = probe.IrradianceHarmonics[6];
            gpuProbes[i].Sh7 = probe.IrradianceHarmonics[7];
            gpuProbes[i].Sh8 = probe.IrradianceHarmonics[8];
        }

        var sceneCenter = Engine.MeshManager.SceneBoundingBox.Center;
        var fallbackProbe = 0;
        for (var i = 1; i < Probes.Count; i++)
        {
            if ((Probes[i].Position - sceneCenter).LengthSquared < (Probes[fallbackProbe].Position - sceneCenter).LengthSquared)
                fallbackProbe = i;
        }

        LightProbesSsbo.UpdateData(new LightProbes
        {
            Probes = gpuProbes,
            ProbeCount = Probes.Count,
            FallbackProbe = fallbackProbe
        });

        ArrayPool<Jellyfish.Render.Shaders.Structs.LightProbe>.Shared.Return(gpuProbes);
    }

    public void UpdateClusterBuffer()
    {
        using var _ = new PerformanceMeasure("ProbeClusters.Update");

        var viewport = Engine.MainViewport;

        LightProbesSsbo.Bind(1);

        _cullingShader.Bind();
        _cullingShader.SetMatrix4("view", viewport.GetViewMatrix());
        _cullingShader.SetMatrix4("invProjection", viewport.GetProjectionMatrix().Inverted());
        _cullingShader.SetFloat("near", viewport.NearPlane);
        _cullingShader.SetFloat("far", viewport.FarPlane);
        _cullingShader.DispatchCompute(1, 1, ClustersZ, MemoryBarrierMask.ShaderStorageBarrierBit);
        _cullingShader.Unbind();
    }

    public void Reset()
    {
        foreach (var lightProbe in Probes)
        {
            lightProbe.Unload();
        }

        Probes.Clear();
        _probesCount = 0;
    }

    public void Unload()
    {
        Reset();
        _cullingShader.Unload();
    }

    public void GenerateProbeGrid()
    {
        // don't inf loop on empty maps
        if (Engine.MeshManager.SceneBoundingBox.Radius < 6)
            return;

        var xStep = (int)Math.Max(1, Engine.MeshManager.SceneBoundingBox.Size.X / 6);
        var yStep = (int)Math.Max(1, Engine.MeshManager.SceneBoundingBox.Size.Y / 4);
        var zStep = (int)Math.Max(1, Engine.MeshManager.SceneBoundingBox.Size.Z / 6);

        for (var xOffset = (int)Engine.MeshManager.SceneBoundingBox.Min.X + xStep;
             xOffset < (int)Engine.MeshManager.SceneBoundingBox.Max.X;
             xOffset += xStep)
        {
            for (var yOffset = (int)Engine.MeshManager.SceneBoundingBox.Min.Y + yStep;
                 yOffset < (int)Engine.MeshManager.SceneBoundingBox.Max.Y;
                 yOffset += yStep)
            {
                for (var zOffset = (int)Engine.MeshManager.SceneBoundingBox.Min.Z + zStep;
                     zOffset < (int)Engine.MeshManager.SceneBoundingBox.Max.Z;
                     zOffset += zStep)
                {
                    var offset = new Vector3(xOffset, yOffset, zOffset);
                    if (!Probes.Any(x => x.Position == offset))
                        AddProbe(offset);
                }
            }
        }
    }
}