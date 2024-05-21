using System;
using System.Linq;
using Jellyfish.Entities;
using Jellyfish.Render.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class Main : Shader
{
    private readonly bool _usePhong;
    private readonly int _phongExponent;
    private readonly Texture _diffuse;
    private readonly Texture? _normal;

    public Main(string diffusePath, string? normalPath = null, bool usePhong = false, int phongExponent = 16) : 
        base("shaders/Main.vert", null, "shaders/Main.frag")
    {
        _usePhong = usePhong;
        _phongExponent = phongExponent;
        _diffuse = new Texture(diffusePath);
        _diffuse.Bind();

        if (!string.IsNullOrEmpty(normalPath))
        {
            _normal = new Texture(normalPath);
            _normal.Bind(TextureUnit.Texture1);
        }

        // move to vertex buffer?
        var vertexLocation = GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

        var texCoordLocation = GetAttribLocation("aTexCoord");
        GL.EnableVertexAttribArray(texCoordLocation);
        GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float),
            3 * sizeof(float));

        var normalLocation = GetAttribLocation("aNormal");
        GL.EnableVertexAttribArray(normalLocation);
        GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float),
            5 * sizeof(float));
    }

    public override void Bind()
    {
        var camera = Camera.Instance;
        if (camera == null)
            return;

        base.Bind();

        SetVector3("cameraPos", camera.GetPropertyValue<Vector3>("Position"));
        SetMatrix4("view", camera.GetViewMatrix());
        SetMatrix4("projection", camera.GetProjectionMatrix());

        var lights = LightManager.Lights.Where(x=> x.Source.Enabled).ToArray();
        SetInt("lightSourcesCount", lights.Length);
        for (var i = 0; i < lights.Length; i++)
        {
            var light = lights[i].Source;
            SetVector3($"lightSources[{i}].position", light.Position);
            SetVector3($"lightSources[{i}].direction", light.Rotation);

            SetVector3($"lightSources[{i}].diffuse", new Vector3(light.Color.R, light.Color.G, light.Color.B));
            SetVector3($"lightSources[{i}].ambient", new Vector3(light.Ambient.R, light.Ambient.G, light.Ambient.B));
            SetFloat($"lightSources[{i}].brightness", light.Color.A);

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

            SetMatrix4($"lightSources[{i}].lightSpaceMatrix", light.Projection());

            if (light.UseShadows)
            {
                GL.ActiveTexture(TextureUnit.Texture2 + i);
                lights[i].ShadowRt!.Bind();
            }
        }

        SetInt("usePhong", _usePhong ? 1 : 0);
        SetInt("phongExponent", _phongExponent);

        _diffuse.Bind();
        _normal?.Bind(TextureUnit.Texture1);
    }

    public override void Unbind()
    {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2D, 0);

        var lights = LightManager.Lights.Where(x => x.Source.Enabled).ToArray();
        SetInt("lightSourcesCount", lights.Length);
        for (var i = 0; i < lights.Length; i++)
        {
            GL.ActiveTexture(TextureUnit.Texture2 + i);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        base.Bind();
    }
}