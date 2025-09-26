using System;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class Skybox : Shader
{
    public Skybox() : base("shaders/Skybox.vert", null, "shaders/Skybox.frag") { }

    public override void Bind()
    {
        base.Bind();

        var rotationVector = Vector3.Transform(Vector3.UnitY, Engine.LightManager.Sun!.Source.Rotation);
        SetVector3("uSunPos", rotationVector);
        SetFloat("uViewHeight", Math.Max(0f, Engine.MainViewport.Position.Y));
        SetFloat("uSunIntensity", Engine.LightManager.Sun.Source.Brightness * 4f);

        SetMatrix4("view", Engine.MainViewport.GetViewMatrix().ClearTranslation());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());
    }
}