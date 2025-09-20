using System;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Render.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class Main : Shader
{
    private readonly bool _alphaTest;
    private readonly Texture? _diffuse;
    private readonly Texture? _normal;
    private readonly Texture? _metRought;

    private readonly Texture? _prefilterMap;
    private readonly Texture? _irradianceMap;

    private const uint sun_shadow_unit = 5;
    private const uint first_light_shadow_unit = sun_shadow_unit + Sun.cascades + 1;

    public Main(Material material) : base("shaders/Main.vert", null, "shaders/Main.frag")
    {
        if (material.TryGetParam<string>("Diffuse", out var diffusePath))
            _diffuse = Engine.TextureManager.GetTexture(new TextureParams {Name = $"{material.Directory}/{diffusePath}", Srgb = true}).Texture;

        if (material.TryGetParam<string>("Normal", out var normalPath))
            _normal = Engine.TextureManager.GetTexture(new TextureParams { Name = $"{material.Directory}/{normalPath}"}).Texture;

        if (material.TryGetParam<string>("MetalRoughness", out var metroughtPath))
            _metRought = Engine.TextureManager.GetTexture(new TextureParams { Name = $"{material.Directory}/{metroughtPath}"}).Texture;

        _prefilterMap = Engine.TextureManager.GetTexture("_rt_Prefilter");
        _irradianceMap = Engine.TextureManager.GetTexture("_rt_Irradiance");

        if (material.TryGetParam<bool>("AlphaTest", out var alphatest))
            _alphaTest = alphatest;
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

        var totalLights = LightManager.Lights.Count;
        for (var i = 0; i < totalLights; i++)
        {
            var light = LightManager.Lights[i].Source;
            if (!light.Enabled)
            {
                continue;
            }

            SetVector3($"lightSources[{i}].position", light.Position);

            var rotationVector = Vector3.Transform(-Vector3.UnitY, light.Rotation);
            SetVector3($"lightSources[{i}].direction", rotationVector);

            SetVector3($"lightSources[{i}].diffuse", new Vector3(light.Color.X, light.Color.Y, light.Color.Z));
            SetVector3($"lightSources[{i}].ambient", new Vector3(light.Ambient.X, light.Ambient.Y, light.Ambient.Z));
            SetFloat($"lightSources[{i}].brightness", light.Brightness);

            var lightType = 0; // point
            if (light is Spotlight)
                lightType = 1;

            SetInt($"lightSources[{i}].type", lightType);

            if (light is PointLight point)
            {
                SetFloat($"lightSources[{i}].constant", point.GetPropertyValue<float>("Constant"));
                SetFloat($"lightSources[{i}].linear", point.GetPropertyValue<float>("Linear"));
                SetFloat($"lightSources[{i}].quadratic", point.GetPropertyValue<float>("Quadratic"));
            }

            if (light is Spotlight spot)
            {
                SetFloat($"lightSources[{i}].constant", spot.GetPropertyValue<float>("Constant"));
                SetFloat($"lightSources[{i}].linear", spot.GetPropertyValue<float>("Linear"));
                SetFloat($"lightSources[{i}].quadratic", spot.GetPropertyValue<float>("Quadratic"));
                SetFloat($"lightSources[{i}].cone", (float)Math.Cos(MathHelper.DegreesToRadians(spot.GetPropertyValue<float>("Cone"))));
                SetFloat($"lightSources[{i}].outcone", (float)Math.Cos(MathHelper.DegreesToRadians(spot.GetPropertyValue<float>("OuterCone"))));
            }

            SetFloat($"lightSources[{i}].near", light.NearPlane);
            SetFloat($"lightSources[{i}].far", light.FarPlane);

            SetMatrix4($"lightSources[{i}].lightSpaceMatrix", light.Projections[0]);

            if (light.UseShadows && LightManager.Lights[i].Shadows.Count > 0)
            {
                BindTexture(first_light_shadow_unit + (uint)i, LightManager.Lights[i].Shadows[0].RenderTarget);
            }

            SetBool($"lightSources[{i}].hasShadows", light.UseShadows && LightManager.Lights[i].Shadows.Count > 0);
            SetBool($"lightSources[{i}].usePcss", light.UseShadows && light.UsePcss);
        }
        SetInt("lightSourcesCount", totalLights);

        if (LightManager.Sun != null && LightManager.Sun.Source.Enabled)
        {
            var sun = LightManager.Sun.Source;

            var rotationVector = Vector3.Transform(-Vector3.UnitY, sun.Rotation);
            SetVector3("sun.direction", rotationVector);

            SetVector3("sun.diffuse", new Vector3(sun.Color.X, sun.Color.Y, sun.Color.Z));
            SetVector3("sun.ambient", new Vector3(sun.Ambient.X, sun.Ambient.Y, sun.Ambient.Z));
            SetFloat("sun.brightness", sun.Brightness);

            for (var i = 0; i < sun.Projections.Length; i++)
            {
                SetMatrix4($"sun.lightSpaceMatrix[{i}]", sun.Projections[i]);
                SetFloat($"sun.cascadeFar[{i}]", Sun.CascadeRanges[i].Far);
                SetFloat($"sun.cascadeNear[{i}]", Sun.CascadeRanges[i].Near);
            }
            SetBool("sun.hasShadows", sun.UseShadows && LightManager.Sun.Shadows.Count > 0);
            SetBool("sun.usePcss", sun.UseShadows && sun.UsePcss);
        }

        if (LightManager.Sun != null && LightManager.Sun.Source.Enabled && LightManager.Sun.Source.UseShadows)
        {
            for (uint i = 0; i < Sun.cascades; i++)
            {
                BindTexture(sun_shadow_unit + i, LightManager.Sun.Shadows[(int)i].RenderTarget);
            }
        }
        SetBool("sunEnabled", LightManager.Sun != null && LightManager.Sun.Source.Enabled);

        SetBool("useNormals", _normal != null);
        SetBool("usePbr", _metRought != null);
        SetBool("alphaTest", _alphaTest);
        SetInt("prefilterMips", _prefilterMap?.Levels ?? 0);
        SetBool("iblEnabled", ConVarStorage.Get<bool>("mat_ibl_enabled"));

        BindTexture(0, _diffuse);
        BindTexture(1, _normal);
        BindTexture(2, _metRought);
        BindTexture(3, _prefilterMap);
        BindTexture(4, _irradianceMap);
    }

    public override void Unload()
    {
        _diffuse?.Unload();
        _normal?.Unload();
        _metRought?.Unload();
        _prefilterMap?.Unload();
        _irradianceMap?.Unload();

        base.Unload();
    }
}