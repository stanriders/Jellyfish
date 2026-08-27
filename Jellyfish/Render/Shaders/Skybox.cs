using System;
using Jellyfish.Entities;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class Skybox(HosekWilkieParams skyParams) : Shader("shaders/Skybox.vert", null, "shaders/Skybox.frag")
{
    public override void Bind()
    {
        base.Bind();

        var sun = (Sun)Engine.LightManager.Sun!.Source;

        var rotationVector = Vector3.Transform(Vector3.UnitY, sun.Rotation);
        SetVector3("uSunPos", rotationVector);

        var view = Engine.MainViewport.GetViewMatrix();
        SetFloat("uSunIntensity", sun.Brightness);
        SetVector3("uSunColor", new Vector3(sun.Color.X, sun.Color.Y, sun.Color.Z));

        SetMatrix4("view", view.ClearTranslation());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());

        SetVector3("A", skyParams.A);
        SetVector3("B", skyParams.B);
        SetVector3("C", skyParams.C);
        SetVector3("D", skyParams.D);
        SetVector3("E", skyParams.E);
        SetVector3("F", skyParams.F);
        SetVector3("G", skyParams.G);
        SetVector3("H", skyParams.H);
        SetVector3("I", skyParams.I);
        SetVector3("Z", skyParams.Z);

        SetFloat("uTime", (float)Engine.ElapsedTime);
        SetFloat("uCoverage", sun.Turbidity / 10);
    }
}