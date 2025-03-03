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

        var lights = LightManager.Lights.Where(x=> x.Source.Enabled).ToArray();
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
            if (light is Sun)
                lightType = 1;
            else if (light is Spotlight)
                lightType = 2;

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

            if (light.UseShadows)
            {
                GL.ActiveTexture(TextureUnit.Texture3 + i);
                lights[i].ShadowRt!.Bind();
            }

            SetBool($"lightSources[{i}].hasShadows", light.UseShadows);
        }

        SetBool("useNormals", _normal != null);
        SetBool("usePbr", _metRought != null);

        _diffuse.Bind(0);
        _normal?.Bind(1);
        _metRought?.Bind(2);
    }

    public override void Unbind()
    {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, 0);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, 0);

        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.Texture2d, 0);

        var lights = LightManager.Lights.Where(x => x.Source.Enabled).ToArray();
        SetInt("lightSourcesCount", lights.Length);
        for (uint i = 0; i < lights.Length; i++)
        {
            GL.ActiveTexture(TextureUnit.Texture3 + i);
            GL.BindTexture(TextureTarget.Texture2d, 0);
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