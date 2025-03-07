using System;
using System.Linq;
using Jellyfish.Entities;
using Jellyfish.Render.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class Main : Shader
{
    private readonly Texture _diffuse;
    private readonly Texture? _normal;
    private readonly Texture? _metRought;
    private readonly Texture _dummyShadow;

    public Main(string diffusePath, string? normalPath = null, string? metroughPath = null) : 
        base("shaders/Main.vert", null, "shaders/Main.frag")
    {
        _diffuse = TextureManager.GetTexture(diffusePath, TextureTarget.Texture2d).Texture;

        if (!string.IsNullOrEmpty(normalPath))
        {
            _normal = TextureManager.GetTexture(normalPath, TextureTarget.Texture2d).Texture;
        }

        if (!string.IsNullOrEmpty(metroughPath))
        {
            _metRought = TextureManager.GetTexture(metroughPath, TextureTarget.Texture2d).Texture;
        }

        var (dummyShadow, alreadyExists) = TextureManager.GetTexture("_rt_dummyShadow", TextureTarget.Texture2d);
        _dummyShadow = dummyShadow;

        if (!alreadyExists)
        {
            GL.BindTexture(TextureTarget.Texture2d, _dummyShadow.Handle);
            GL.TextureStorage2D(_dummyShadow.Handle, 1, SizedInternalFormat.DepthComponent24, 1, 1);
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
        }
    }

    public override void Bind()
    {
        var player = Player.Instance;
        if (player == null)
            return;

        base.Bind();

        SetVector3("cameraPos", player.GetPropertyValue<Vector3>("Position"));
        SetMatrix4("view", player.GetViewMatrix());
        SetMatrix4("projection", player.GetProjectionMatrix());

        var lights = LightManager.Lights.Where(x=> x.Source.Enabled).OrderByDescending(x=> x.Source.UseShadows).ToArray(); // this is probably inefficient considering we're running this multiple times a frame
        SetInt("lightSourcesCount", lights.Length);
        for (uint i = 0; i < lights.Length; i++)
        {
            var light = lights[i].Source;
            SetVector3($"lightSources[{i}].position", light.Position);

            var rotationVector = Vector3.Transform(-Vector3.UnitY, light.Rotation);
            SetVector3($"lightSources[{i}].direction", rotationVector);

            SetVector3($"lightSources[{i}].diffuse", new Vector3(light.Color.X, light.Color.Y, light.Color.Z));
            SetVector3($"lightSources[{i}].ambient", new Vector3(light.Ambient.X, light.Ambient.Y, light.Ambient.Z));
            SetFloat($"lightSources[{i}].brightness", light.Color.W);

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

            SetMatrix4($"lightSources[{i}].lightSpaceMatrix", light.Projection);

            if (light.UseShadows && lights[i].ShadowRt != null)
            {
                lights[i].ShadowRt!.Bind(4 + i);
            }

            SetBool($"lightSources[{i}].hasShadows", light.UseShadows && lights[i].ShadowRt != null);
        }

        if (LightManager.Sun != null && LightManager.Sun.Source.Enabled)
        {
            var light = LightManager.Sun.Source;

            var rotationVector = Vector3.Transform(-Vector3.UnitY, light.Rotation);
            SetVector3("sun.direction", rotationVector);

            SetVector3("sun.diffuse", new Vector3(light.Color.X, light.Color.Y, light.Color.Z));
            SetVector3("sun.ambient", new Vector3(light.Ambient.X, light.Ambient.Y, light.Ambient.Z));
            SetFloat("sun.brightness", light.Color.W);

            SetMatrix4("sun.lightSpaceMatrix", light.Projection);
            SetBool("sun.hasShadows", light.UseShadows && LightManager.Sun.ShadowRt != null);
        }

        if (LightManager.Sun != null && LightManager.Sun.Source.Enabled && LightManager.Sun.Source.UseShadows)
        {
            LightManager.Sun.ShadowRt!.Bind(3);
        }
        else
        {
            _dummyShadow.Bind(3);
        }
        SetBool("sunEnabled", LightManager.Sun != null && LightManager.Sun.Source.Enabled);

        SetBool("useNormals", _normal != null);
        SetBool("usePbr", _metRought != null);

        _diffuse.Bind(0);
        _normal?.Bind(1);
        _metRought?.Bind(2);

        var workingLights = (uint)lights.Count(x => x.Source.Enabled && x.Source.UseShadows && x.ShadowRt != null);
        for (uint i = workingLights; i < LightManager.max_lights; i++)
        {
            _dummyShadow.Bind(4 + i);
        }
    }

    public override void Unbind()
    {
        GL.BindTextureUnit(0, 0);
        GL.BindTextureUnit(1, 0);
        GL.BindTextureUnit(2, 0);

        if (LightManager.Sun != null)
        {
            GL.BindTextureUnit(3, 0);
        }

        for (uint i = 0; i < LightManager.max_lights; i++)
        {
            GL.BindTextureUnit(4 + i, 0);
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