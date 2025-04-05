using System;
using System.Collections.Generic;
using Jellyfish.Entities;
using Jellyfish.Render.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class Main : Shader
{
    private readonly bool _alphaTest;
    private readonly Texture _diffuse;
    private readonly Texture? _normal;
    private readonly Texture? _metRought;
    private readonly Texture _dummyShadow;

    private const uint sun_shadow_unit = 3;
    private const uint first_light_shadow_unit = 4;

    public Main(string diffusePath, string? normalPath = null, string? metroughPath = null, bool alphaTest = false) : 
        base("shaders/Main.vert", null, "shaders/Main.frag")
    {
        _alphaTest = alphaTest;
        _diffuse = TextureManager.GetTexture(diffusePath, TextureTarget.Texture2d, true).Texture;

        if (!string.IsNullOrEmpty(normalPath))
        {
            _normal = TextureManager.GetTexture(normalPath, TextureTarget.Texture2d, false).Texture;
        }

        if (!string.IsNullOrEmpty(metroughPath))
        {
            _metRought = TextureManager.GetTexture(metroughPath, TextureTarget.Texture2d, false).Texture;
        }

        var (dummyShadow, alreadyExists) = TextureManager.GetTexture("_rt_dummyShadow", TextureTarget.Texture2d, false);
        _dummyShadow = dummyShadow;

        if (!alreadyExists)
        {
            const SizedInternalFormat format = SizedInternalFormat.DepthComponent24;
            const int levels = 1;

            GL.BindTexture(TextureTarget.Texture2d, _dummyShadow.Handle);
            GL.TextureStorage2D(_dummyShadow.Handle, levels, format, 1, 1);
            GL.TextureParameteri(_dummyShadow.Handle, TextureParameterName.TextureMinFilter,
                new[] { (int)TextureMinFilter.Nearest });
            GL.TextureParameteri(_dummyShadow.Handle, TextureParameterName.TextureMagFilter,
                new[] { (int)TextureMinFilter.Nearest });
            GL.TextureParameteri(_dummyShadow.Handle, TextureParameterName.TextureWrapS,
                new[] { (int)TextureWrapMode.ClampToBorder });
            GL.TextureParameteri(_dummyShadow.Handle, TextureParameterName.TextureWrapT,
                new[] { (int)TextureWrapMode.ClampToBorder });

            GL.TextureParameteri(_dummyShadow.Handle, TextureParameterName.TextureCompareMode,
                (int)TextureCompareMode.CompareRefToTexture);
            GL.TextureParameteri(_dummyShadow.Handle, TextureParameterName.TextureCompareFunc,
                (int)DepthFunction.Lequal);

            GL.TextureParameterf(_dummyShadow.Handle, TextureParameterName.TextureBorderColor,
                new[] { 1f, 1f, 1f, 1f });
            GL.BindTexture(TextureTarget.Texture2d, 0);

            dummyShadow.Levels = levels;
            dummyShadow.Format = format.ToString();
        }
    }

    public override void Bind()
    {
        base.Bind();

        SetVector3("cameraPos", Camera.Instance.Position);
        SetMatrix4("view", Camera.Instance.GetViewMatrix());
        SetMatrix4("projection", Camera.Instance.GetProjectionMatrix());

        var unitsRequiringDummyShadows = new List<uint>();
        var totalLights = LightManager.Lights.Count;
        for (var i = 0; i < totalLights; i++)
        {
            var light = LightManager.Lights[i].Source;
            if (!light.Enabled)
            {
                unitsRequiringDummyShadows.Add(first_light_shadow_unit + (uint)i);
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

            SetMatrix4($"lightSources[{i}].lightSpaceMatrix", light.Projections[0]);

            if (light.UseShadows && LightManager.Lights[i].Shadows.Count > 0)
            {
                LightManager.Lights[i].Shadows[0].RenderTarget.Bind(first_light_shadow_unit + (uint)i);
            }
            else
            {
                unitsRequiringDummyShadows.Add(first_light_shadow_unit + (uint)i);
            }

            SetBool($"lightSources[{i}].hasShadows", light.UseShadows && LightManager.Lights[i].Shadows.Count > 0);
        }
        SetInt("lightSourcesCount", totalLights);

        for (var i = totalLights; i < LightManager.max_lights; i++)
        {
            unitsRequiringDummyShadows.Add(first_light_shadow_unit + (uint)i);
        }

        if (LightManager.Sun != null && LightManager.Sun.Source.Enabled)
        {
            var light = LightManager.Sun.Source;

            var rotationVector = Vector3.Transform(-Vector3.UnitY, light.Rotation);
            SetVector3("sun.direction", rotationVector);

            SetVector3("sun.diffuse", new Vector3(light.Color.X, light.Color.Y, light.Color.Z));
            SetVector3("sun.ambient", new Vector3(light.Ambient.X, light.Ambient.Y, light.Ambient.Z));
            SetFloat("sun.brightness", light.Brightness);

            SetMatrix4("sun.lightSpaceMatrix", light.Projections[0]);
            SetBool("sun.hasShadows", light.UseShadows && LightManager.Sun.Shadows.Count > 0);
        }

        if (LightManager.Sun != null && LightManager.Sun.Source.Enabled && LightManager.Sun.Source.UseShadows)
        {
            LightManager.Sun.Shadows[0].RenderTarget.Bind(sun_shadow_unit);
        }
        else
        {
            unitsRequiringDummyShadows.Add(sun_shadow_unit);
        }
        SetBool("sunEnabled", LightManager.Sun != null && LightManager.Sun.Source.Enabled);

        SetBool("useNormals", _normal != null);
        SetBool("usePbr", _metRought != null);
        SetBool("alphaTest", _alphaTest);

        _diffuse.Bind(0);
        _normal?.Bind(1);
        _metRought?.Bind(2);

        foreach (var unit in unitsRequiringDummyShadows)
        {
            _dummyShadow.Bind(unit);
        }
    }

    public override void Unbind()
    {
        GL.BindTextureUnit(0, 0);
        GL.BindTextureUnit(1, 0);
        GL.BindTextureUnit(2, 0);

        if (LightManager.Sun != null)
        {
            GL.BindTextureUnit(sun_shadow_unit, 0);
        }

        for (uint i = 0; i < LightManager.max_lights; i++)
        {
            GL.BindTextureUnit(first_light_shadow_unit + i, 0);
        }

        base.Unbind();
    }

    public override void Unload()
    {
        _diffuse.Unload();
        _normal?.Unload();
        _metRought?.Unload();

        base.Unload();
    }
}