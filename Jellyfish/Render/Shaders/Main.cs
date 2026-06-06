using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Render.Lighting;
using Jellyfish.Render.Shaders.Structs;
using OpenTK.Mathematics;
using System;
using System.Buffers;
using System.Diagnostics;
using LightProbe = Jellyfish.Render.Lighting.LightProbe;
using Sun = Jellyfish.Entities.Sun;

namespace Jellyfish.Render.Shaders;

public class Main : Shader
{
    private readonly bool _hasTransparency;
    private readonly Texture? _diffuse;
    private readonly Texture? _normal;
    private readonly Texture? _metRought;
    private readonly Texture? _reflectionMap;

    public Main(Material material) : base("shaders/Main.vert", null, "shaders/Main.frag")
    {
        if (material.TryGetParam<string>("Diffuse", out var diffusePath))
            _diffuse = Engine.TextureManager.GetTexture(new TextureParams {Name = $"{material.Directory}/{diffusePath}", Srgb = true}).Texture;

        if (material.TryGetParam<string>("Normal", out var normalPath))
            _normal = Engine.TextureManager.GetTexture(new TextureParams { Name = $"{material.Directory}/{normalPath}"}).Texture;

        if (material.TryGetParam<string>("MetalRoughness", out var metroughtPath))
            _metRought = Engine.TextureManager.GetTexture(new TextureParams { Name = $"{material.Directory}/{metroughtPath}"}).Texture;

        _reflectionMap = Engine.TextureManager.GetTexture("_rt_ReflectionsBlurY");

        if (material.TryGetParam<bool>("AlphaTest", out var hasTransparency))
            _hasTransparency = hasTransparency;
    }

    public Main(Texture diffuse) : base("shaders/Main.vert", null, "shaders/Main.frag")
    {
        _diffuse = diffuse;
    }

    public override void Bind()
    {
        base.Bind();

        SetVector3("cameraPos", Engine.MainViewport.Position);
        SetMatrix4("view", Engine.MainViewport.GetViewMatrix());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());

        var lights = ArrayPool<Light>.Shared.Rent(LightManager.max_lights);
        var sunCascadeTextures = ArrayPool<ulong>.Shared.Rent(Sun.cascades);
        var sunProjections = ArrayPool<Matrix4>.Shared.Rent(Sun.cascades);
        var cascadeRangesFar = ArrayPool<int>.Shared.Rent(Sun.cascades);
        var cascadeRangesNear = ArrayPool<int>.Shared.Rent(Sun.cascades);

        var totalLights = Engine.LightManager.Lights.Count;
        var lightSourcesStruct = new LightSources
        {
            Lights = lights,
            Sun = new Structs.Sun { ShadowTexture = sunCascadeTextures },
            SunEnabled = Engine.LightManager.Sun != null && Engine.LightManager.Sun.Source.Enabled ? 1 : 0
        };

        var currentLight = 0;
        for (var i = 0; i < totalLights; i++)
        {
            var source = Engine.LightManager.Lights[i].Source;
            if (!source.Enabled)
            {
                continue;
            }

            lightSourcesStruct.Lights[currentLight].Position = new Vector4(source.Position);

            var rotationVector = Vector3.Transform(-Vector3.UnitY, source.Rotation);
            lightSourcesStruct.Lights[currentLight].Direction = new Vector4(rotationVector);

            lightSourcesStruct.Lights[currentLight].Diffuse = new Vector4(source.Color.X, source.Color.Y, source.Color.Z, 0);

            lightSourcesStruct.Lights[currentLight].Brightness = source.Brightness;

            var lightType = 0; // point
            if (source is Spotlight)
                lightType = 1;

            lightSourcesStruct.Lights[currentLight].Type = lightType;

            if (source is PointLight point)
            {
                lightSourcesStruct.Lights[currentLight].Constant = point.GetPropertyValue<float>("Constant");
                lightSourcesStruct.Lights[currentLight].Linear = point.GetPropertyValue<float>("Linear");
                lightSourcesStruct.Lights[currentLight].Quadratic = point.GetPropertyValue<float>("Quadratic");
            }

            if (source is Spotlight spot)
            {
                lightSourcesStruct.Lights[currentLight].Constant = spot.GetPropertyValue<float>("Constant");
                lightSourcesStruct.Lights[currentLight].Linear = spot.GetPropertyValue<float>("Linear");
                lightSourcesStruct.Lights[currentLight].Quadratic = spot.GetPropertyValue<float>("Quadratic");
                lightSourcesStruct.Lights[currentLight].Cone = (float)Math.Cos(MathHelper.DegreesToRadians(spot.GetPropertyValue<float>("Cone")));
                lightSourcesStruct.Lights[currentLight].Outcone = (float)Math.Cos(MathHelper.DegreesToRadians(spot.GetPropertyValue<float>("OuterCone")));
            }

            lightSourcesStruct.Lights[currentLight].Near = source.NearPlane;
            lightSourcesStruct.Lights[currentLight].Far = source.FarPlane;

            lightSourcesStruct.Lights[currentLight].LightSpaceMatrix = source.Projection(0);

            lightSourcesStruct.Lights[currentLight].HasShadows = source.UseShadows && Engine.LightManager.Lights[i].Shadows.Count > 0 ? 1 : 0;
            lightSourcesStruct.Lights[currentLight].UsePcss = source.UseShadows && source.UsePcss ? 1 : 0;

            if (source.UseShadows && Engine.LightManager.Lights[i].Shadows.Count > 0)
            {
                lightSourcesStruct.Lights[currentLight].ShadowTexture = Engine.LightManager.Lights[i].Shadows[0].BindlessHandle;
            }

            currentLight++;
        }

        lightSourcesStruct.LightsCount = currentLight;

        if (Engine.LightManager.Sun != null && Engine.LightManager.Sun.Source.Enabled)
        {
            var sun = Engine.LightManager.Sun.Source;

            lightSourcesStruct.Sun.Diffuse = new Vector4(sun.Color.X, sun.Color.Y, sun.Color.Z, 0);

            var rotationVector = Vector3.Transform(-Vector3.UnitY, sun.Rotation);
            lightSourcesStruct.Sun.Direction = new Vector4(rotationVector);

            lightSourcesStruct.Sun.Brightness = sun.Brightness;

            for (var i = 0; i < Sun.cascades; i++)
            {
                sunProjections[i] = sun.Projection(i);
                cascadeRangesFar[i] = Sun.CascadeRanges[i].Far;
                cascadeRangesNear[i] = Sun.CascadeRanges[i].Near;
            }

            lightSourcesStruct.Sun.LightSpaceMatrix = sunProjections;
            lightSourcesStruct.Sun.CascadeFar = cascadeRangesFar;
            lightSourcesStruct.Sun.CascadeNear = cascadeRangesNear;

            lightSourcesStruct.Sun.HasShadows = sun.UseShadows && Engine.LightManager.Sun.Shadows.Count > 0 ? 1 : 0;
            lightSourcesStruct.Sun.UsePcss = sun.UseShadows && sun.UsePcss ? 1 : 0;

            if (Engine.LightManager.Sun.Source.UseShadows)
            {
                for (var i = 0; i < Sun.cascades; i++)
                {
                    lightSourcesStruct.Sun.ShadowTexture[i] = Engine.LightManager.Sun.Shadows[i].BindlessHandle;
                }
            }
        }

        SetBool("useNormals", _normal != null);
        SetBool("usePbr", _metRought != null);
        SetBool("useTransparency", _hasTransparency);
        SetInt("prefilterMips", LightProbe.PrefilterMips);
        SetBool("iblEnabled", ConVarStorage.Get<bool>("mat_ibl_enabled"));
        SetBool("iblPrefilterEnabled", ConVarStorage.Get<bool>("mat_ibl_prefilter"));
        SetBool("sslrEnabled", ConVarStorage.Get<bool>("mat_sslr_enabled"));
        SetVector2("screenSize", Engine.MainViewport.Size);

        Engine.LightManager.LightSourcesSsbo.UpdateData(lightSourcesStruct);
        Engine.LightManager.LightSourcesSsbo.Bind(0);
        Engine.Renderer.ImageBasedLighting?.LightProbesSsbo.Bind(1);

        BindTexture(0, _diffuse);
        BindTexture(1, _normal);
        BindTexture(2, _metRought);
        BindTexture(3, _reflectionMap);

        ArrayPool<Light>.Shared.Return(lights, true);
        ArrayPool<ulong>.Shared.Return(sunCascadeTextures);
        ArrayPool<Matrix4>.Shared.Return(sunProjections);
        ArrayPool<int>.Shared.Return(cascadeRangesNear);
        ArrayPool<int>.Shared.Return(cascadeRangesFar);
    }

    public override void Unload()
    {
        _diffuse?.Unload();
        _normal?.Unload();
        _metRought?.Unload();

        base.Unload();
    }
}