using System;
using System.Linq;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Render.Lighting;
using Jellyfish.Render.Shaders.Structs;
using OpenTK.Mathematics;
using Sun = Jellyfish.Entities.Sun;

namespace Jellyfish.Render.Shaders;

public class Main : Shader
{
    private readonly bool _alphaTest;
    private readonly Texture? _diffuse;
    private readonly Texture? _normal;
    private readonly Texture? _metRought;

    private readonly Texture? _prefilterMap;
    private readonly Texture? _irradianceMap;
    private readonly Texture? _reflectionMap;

    private const uint sun_shadow_unit = 6;
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
        _reflectionMap = Engine.TextureManager.GetTexture("_rt_ReflectionsBlurY");

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

        var lightSourcesStruct = new LightSources
        {
            Lights = new Light[LightManager.max_lights],
            Sun = new Structs.Sun()
        };

        var totalLights = Engine.LightManager.Lights.Count;
        for (var i = 0; i < totalLights; i++)
        {
            var light = Engine.LightManager.Lights[i].Source;
            if (!light.Enabled)
            {
                continue;
            }

            lightSourcesStruct.Lights[i].Position = new Vector4(light.Position);

            var rotationVector = Vector3.Transform(-Vector3.UnitY, light.Rotation);
            lightSourcesStruct.Lights[i].Direction = new Vector4(rotationVector);

            lightSourcesStruct.Lights[i].Diffuse = new Vector4(light.Color.X, light.Color.Y, light.Color.Z, 0);
            lightSourcesStruct.Lights[i].Ambient = new Vector4(light.Ambient.X, light.Ambient.Y, light.Ambient.Z, 0);

            lightSourcesStruct.Lights[i].Brightness = light.Brightness;

            var lightType = 0; // point
            if (light is Spotlight)
                lightType = 1;

            lightSourcesStruct.Lights[i].Type = lightType;

            if (light is PointLight point)
            {
                lightSourcesStruct.Lights[i].Constant = point.GetPropertyValue<float>("Constant");
                lightSourcesStruct.Lights[i].Linear = point.GetPropertyValue<float>("Linear");
                lightSourcesStruct.Lights[i].Quadratic = point.GetPropertyValue<float>("Quadratic");
            }

            if (light is Spotlight spot)
            {
                lightSourcesStruct.Lights[i].Constant = spot.GetPropertyValue<float>("Constant");
                lightSourcesStruct.Lights[i].Linear = spot.GetPropertyValue<float>("Linear");
                lightSourcesStruct.Lights[i].Quadratic = spot.GetPropertyValue<float>("Quadratic");
                lightSourcesStruct.Lights[i].Cone = (float)Math.Cos(MathHelper.DegreesToRadians(spot.GetPropertyValue<float>("Cone")));
                lightSourcesStruct.Lights[i].Outcone = (float)Math.Cos(MathHelper.DegreesToRadians(spot.GetPropertyValue<float>("OuterCone")));
            }

            lightSourcesStruct.Lights[i].Near = light.NearPlane;
            lightSourcesStruct.Lights[i].Far = light.FarPlane;

            lightSourcesStruct.Lights[i].LightSpaceMatrix = light.Projections[0];

            lightSourcesStruct.Lights[i].HasShadows = light.UseShadows && Engine.LightManager.Lights[i].Shadows.Count > 0;
            lightSourcesStruct.Lights[i].UsePcss = light.UseShadows && light.UsePcss;

            if (light.UseShadows && Engine.LightManager.Lights[i].Shadows.Count > 0)
            {
                BindTexture(first_light_shadow_unit + (uint)i, Engine.LightManager.Lights[i].Shadows[0].RenderTarget);
            }
        }
        SetInt("lightSourcesCount", totalLights);

        if (Engine.LightManager.Sun != null && Engine.LightManager.Sun.Source.Enabled)
        {
            var sun = Engine.LightManager.Sun.Source;

            lightSourcesStruct.Sun.Diffuse = new Vector4(sun.Color.X, sun.Color.Y, sun.Color.Z, 0);
            lightSourcesStruct.Sun.Ambient = new Vector4(sun.Ambient.X, sun.Ambient.Y, sun.Ambient.Z, 0);

            var rotationVector = Vector3.Transform(-Vector3.UnitY, sun.Rotation);
            lightSourcesStruct.Sun.Direction = new Vector4(rotationVector);

            lightSourcesStruct.Sun.Brightness = sun.Brightness;
            lightSourcesStruct.Sun.LightSpaceMatrix = sun.Projections.ToArray();
            lightSourcesStruct.Sun.CascadeFar = Sun.CascadeRanges.Select(x=> x.Far).ToArray();
            lightSourcesStruct.Sun.CascadeNear = Sun.CascadeRanges.Select(x => x.Near).ToArray();

            lightSourcesStruct.Sun.HasShadows = sun.UseShadows && Engine.LightManager.Sun.Shadows.Count > 0 ? 1 : 0;
            lightSourcesStruct.Sun.UsePcss = sun.UseShadows && sun.UsePcss ? 1 : 0;

            if (Engine.LightManager.Sun.Source.UseShadows)
            {
                for (uint i = 0; i < Sun.cascades; i++)
                {
                    BindTexture(sun_shadow_unit + i, Engine.LightManager.Sun.Shadows[(int)i].RenderTarget);
                }
            }
        }
        SetBool("sunEnabled", Engine.LightManager.Sun != null && Engine.LightManager.Sun.Source.Enabled);

        SetBool("useNormals", _normal != null);
        SetBool("usePbr", _metRought != null);
        SetBool("useTransparency", _alphaTest);
        SetInt("prefilterMips", _prefilterMap?.Levels ?? 0);
        SetBool("iblEnabled", ConVarStorage.Get<bool>("mat_ibl_enabled"));
        SetBool("sslrEnabled", ConVarStorage.Get<bool>("mat_sslr_enabled"));
        SetVector2("screenSize", new Vector2(Engine.MainViewport.Size.X, Engine.MainViewport.Size.Y));

        Engine.LightManager.LightSourcesSsbo.UpdateData(lightSourcesStruct);
        Engine.LightManager.LightSourcesSsbo.Bind(0);

        BindTexture(0, _diffuse);
        BindTexture(1, _normal);
        BindTexture(2, _metRought);
        BindTexture(3, _prefilterMap);
        BindTexture(4, _irradianceMap);
        BindTexture(5, _reflectionMap);
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