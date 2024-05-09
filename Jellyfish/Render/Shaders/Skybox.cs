using Jellyfish.Entities;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Render.Shaders;

public class Skybox : Shader
{
    public Skybox() :
        base("shaders/Skybox.vert", null, "shaders/Skybox.frag")
    {
        var vertexLocation = GetAttribLocation("aPosition");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
    }

    public override void Bind()
    {
        var camera = EntityManager.FindEntity("camera") as Camera;
        if (camera == null)
        {
            Log.Error("[Skybox] Camera doesn't exist!");
            return;
        }

        var sun = EntityManager.FindEntity("light_sun") as Sun;
        if (sun == null)
        {
            Log.Error("[Skybox] No sun!");
            return;
        }

        base.Bind();
        
        SetVector3("uSunPos", sun.GetPropertyValue<Vector3>("Direction"));

        var proj = camera.GetProjectionMatrix();
        proj.Transpose();

        SetMatrix4("view", camera.GetViewMatrix().ClearTranslation().Inverted());
        SetMatrix4("projection", proj);
    }
}