using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfish.Render;
using Jellyfish.Render.Lighting;
using Jellyfish.Utils;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_sun")]
public class Sun : BaseEntity, ILightSource
{
    public Sun()
    {
        AddProperty("Color", new Color3<Rgb>(1, 1, 1));
        AddProperty("Ambient", new Color3<Rgb>(0.1f, 0.1f, 0.1f));
        AddProperty("Brightness", 1f);
        AddProperty("Enabled", true);
        AddProperty("Shadows", true);
        AddProperty("PCSS", false);
    }

    public override void Load()
    {
        base.Load();
        Engine.LightManager.AddLight(this);
    }

    public override void Unload()
    {
        Engine.LightManager.RemoveLight(this);
        base.Unload();
    }

    public Vector3 Position => Vector3.Zero;
    public Quaternion Rotation => GetPropertyValue<Quaternion>("Rotation");
    public Color3<Rgb> Color => GetPropertyValue<Color3<Rgb>>("Color");
    public Color3<Rgb> Ambient => GetPropertyValue<Color3<Rgb>>("Ambient");
    public float Brightness => GetPropertyValue<float>("Brightness");
    public bool Enabled => GetPropertyValue<bool>("Enabled");
    public bool UseShadows => GetPropertyValue<bool>("Shadows");
    public float NearPlane => 0;
    public float FarPlane => 0;
    public bool UsePcss => GetPropertyValue<bool>("PCSS");
    public int ShadowResolution => 2048;

    public const int cascades = 4;

    public static (int Near, int Far)[] CascadeRanges =
    [
        (1, 200),
        (200, 1000),
        (1000, 3000),
        (3000, 10000)
    ];

    public List<Matrix4> Projections
    {
        get
        {
            var projections = new List<Matrix4>(cascades);

            for (var i = 0; i < cascades; i++)
            {
                var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Engine.MainViewport.Fov), 
                    Engine.MainViewport.AspectRatio, 
                    CascadeRanges[i].Near, 
                    CascadeRanges[i].Far);

                var frustum = new Frustum(Engine.MainViewport.GetViewMatrix() * projection);

                var direction = Vector3.Transform(Vector3.UnitY, Rotation).Normalized();

                var lightView = Matrix4.LookAt(frustum.Center + direction, frustum.Center, Vector3.UnitY);

                var min = new Vector3(float.MaxValue);
                var max = new Vector3(float.MinValue);

                foreach (var v in frustum.Corners)
                {
                    var trf = Vector3.TransformPosition(v, lightView);
                    min = Vector3.ComponentMin(min, trf);
                    max = Vector3.ComponentMax(max, trf);
                }

                // pullback factor
                const float zMult = 10.0f;
                min.Z = min.Z < 0 ? min.Z * zMult : min.Z / zMult;
                max.Z = max.Z < 0 ? max.Z / zMult : max.Z * zMult;

                var lightProjection = Matrix4.CreateOrthographicOffCenter(min.X, max.X, min.Y, max.Y, min.Z, max.Z);
                projections.Add(lightView * lightProjection);
            }

            return projections;
        }
    }
}