using Jellyfish.Entities;
using Jellyfish.Render.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Serilog;

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
        var camera = EntityManager.FindEntity("camera") as Camera;
        if (camera == null)
        {
            Log.Error("Camera doesn't exist!");
            return;
        }

        base.Bind();

        SetVector3("cameraPos", camera.GetPropertyValue<Vector3>("Position"));
        SetMatrix4("view", camera.GetViewMatrix());
        SetMatrix4("projection", camera.GetProjectionMatrix());

        var lights = LightManager.GetLightSources();
        SetInt("lightSourcesCount", lights.Length);
        for (var i = 0; i < lights.Length; i++)
        {
            SetVector3($"lightSources[{i}].position", lights[i].Position);
            
            SetVector3($"lightSources[{i}].diffuse", new Vector3(lights[i].Color.R, lights[i].Color.G, lights[i].Color.B));
            SetVector3($"lightSources[{i}].ambient", new Vector3(0.1f, 0.1f, 0.1f));
            SetFloat($"lightSources[{i}].brightness", lights[i].Color.A);
            SetInt($"lightSources[{i}].isSun", lights[i] is Sun ? 1 : 0);

            if (lights[i] is PointLight point)
            {
                SetFloat($"lightSources[{i}].constant", point.GetPropertyValue<float>("Constant"));
                SetFloat($"lightSources[{i}].linear", point.GetPropertyValue<float>("Linear"));
                SetFloat($"lightSources[{i}].quadratic", point.GetPropertyValue<float>("Quadratic"));
            }

            if (lights[i] is Sun sun)
            {
                SetVector3($"lightSources[{i}].direction", sun.GetPropertyValue<Vector3>("Direction"));
            }
        }

        SetInt("usePhong", _usePhong ? 1 : 0);
        SetInt("phongExponent", _phongExponent);

        _diffuse.Bind();
        _normal?.Bind(TextureUnit.Texture1);
    }
}