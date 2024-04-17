using Jellyfish.Render.Lighting;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Render.Shaders;

public class Main : Shader
{
    private readonly Texture _diffuse;
    private readonly Texture? _normal;

    public Main(string diffusePath, string? normalPath = null) : base("shaders/Main.vert", null, "shaders/Main.frag")
    {
        _diffuse = new Texture(diffusePath);
        _diffuse.Draw();

        if (!string.IsNullOrEmpty(normalPath))
        {
            _normal = new Texture(normalPath);
            _normal.Draw();
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

    public override void Draw()
    {
        var camera = EntityManager.FindEntity("camera") as Camera;
        if (camera == null)
        {
            Log.Error("Camera doesn't exist!");
            return;
        }

        //SetVector3("cameraPos", Camera.Position);
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

            if (lights[i] is PointLight point)
            {
                SetFloat($"lightSources[{i}].constant", point.Constant);
                SetFloat($"lightSources[{i}].linear", point.Linear);
                SetFloat($"lightSources[{i}].quadratic", point.Quadratic);
            }
        }

        _diffuse.Draw();
        _normal?.Draw(TextureUnit.Texture1);
        base.Draw();
    }
}