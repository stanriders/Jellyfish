using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfish.Render;
using Jellyfish.Render.Lighting;
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
        LightManager.AddLight(this);
    }

    public override void Unload()
    {
        LightManager.RemoveLight(this);
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
                var frustum = Engine.MainViewport.GetFrustum(CascadeRanges[i].Near, CascadeRanges[i].Far);

                var center = frustum.Corners.Aggregate(new Vector3(0, 0, 0), (current, v) => current + v) / frustum.Corners.Length;

                var direction = Vector3.Transform(Vector3.UnitY, Rotation).Normalized();

                var lightView = Matrix4.LookAt(center + direction, center, Vector3.UnitY);

                var minX = float.MaxValue;
                var maxX = float.MinValue;
                var minY = float.MaxValue;
                var maxY = float.MinValue;
                var minZ = float.MaxValue;
                var maxZ = float.MinValue;

                foreach (var v in frustum.Corners)
                {
                    var trf = Vector3.TransformPosition(v, lightView);
                    minX = Math.Min(minX, trf.X);
                    maxX = Math.Max(maxX, trf.X);
                    minY = Math.Min(minY, trf.Y);
                    maxY = Math.Max(maxY, trf.Y);
                    minZ = Math.Min(minZ, trf.Z);
                    maxZ = Math.Max(maxZ, trf.Z);
                }

                // pullback factor
                const float zMult = 10.0f;
                minZ = minZ < 0 ? minZ * zMult : minZ / zMult;
                maxZ = maxZ < 0 ? maxZ / zMult : maxZ * zMult;

                var lightProjection = Matrix4.CreateOrthographicOffCenter(minX, maxX, minY, maxY, minZ, maxZ);
                projections.Add(lightView * lightProjection);
            }

            return projections;
        }
    }
}