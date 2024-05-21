using Jellyfish.Entities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Render.Shaders;

public class Skybox : Shader
{
    private Sun? _sun;
    private bool _noSun;

    public Skybox() :
        base("shaders/Skybox.vert", null, "shaders/Skybox.frag")
    {
        var vertexLocation = GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    }

    public override void Bind()
    {
        var camera = Camera.Instance;
        if (camera == null)
            return;

        if (_sun == null)
        {
            _sun = EntityManager.FindEntity("light_sun") as Sun;
            if (_sun == null && !_noSun)
            {
                Log.Error("[Skybox] No sun, sky won't be rendered!");
                _noSun = true;
                return;
            }
        }

        if (_noSun)
            return;

        base.Bind();
        
        SetVector3("uSunPos", _sun!.GetPropertyValue<Vector3>("Rotation"));

        var proj = camera.GetProjectionMatrix();
        proj.Transpose();

        SetMatrix4("view", camera.GetViewMatrix().ClearTranslation().Inverted());
        SetMatrix4("projection", proj);
    }
}