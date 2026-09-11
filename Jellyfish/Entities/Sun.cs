using Jellyfish.Render.Lighting;
using Jellyfish.Utils;
using OpenTK.Mathematics;
using System;
using Jellyfish.Render;

namespace Jellyfish.Entities;

[Entity("light_sun")]
public class Sun : BaseEntity, ILightSource
{
    public Sun()
    {
        //AddProperty("Color", new Color3<Rgb>(1, 1, 1));
        AddProperty("Brightness", 1f);
        AddProperty("Enabled", true);
        AddProperty("Shadows", true);
        AddProperty("PCSS", false);
        AddProperty("BackCulling", false);

        AddProperty("Albedo", 0.0f);
        AddProperty("Turbidity", 2.0f);
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

    public Color3<Rgb> Color
    {
        get
        {
            var sunDirection = Vector3.Transform(Vector3.UnitY, Rotation);
            var sunTheta = MathF.Acos(Math.Clamp(sunDirection.Y, 0.0f, 1.0f));

            return Sky.CalculateSunColor(sunTheta, Turbidity);
        }
    }

    public float Brightness => GetPropertyValue<float>("Brightness");
    public bool Enabled => GetPropertyValue<bool>("Enabled");
    public bool UseShadows => GetPropertyValue<bool>("Shadows");
    public float NearPlane => 0;
    public float FarPlane => 0;
    public bool UsePcss => GetPropertyValue<bool>("PCSS");
    public bool UseBackCulling => GetPropertyValue<bool>("BackCulling");
    public int ShadowResolution => 2048;

    public float Albedo => GetPropertyValue<float>("Albedo");
    public float Turbidity => GetPropertyValue<float>("Turbidity");

    public const int cascades = 4;

    public static (int Near, int Far)[] CascadeRanges =
    [
        (0, 200),
        (200, 1000),
        (1000, 3000),
        (3000, int.MaxValue)
    ];

    public int ProjectionCount => cascades;

    public Matrix4 Projection(int index)
    {
        var near = MathF.Max(CascadeRanges[index].Near, Engine.MainViewport.NearPlane);
        var far = MathF.Min(CascadeRanges[index].Far, Engine.MainViewport.FarPlane);

        // Extend the slice past its switch point so the shader has room to blend across the seam
        //far = MathF.Min(far * 1.05f, Engine.MainViewport.FarPlane);

        var projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(Engine.MainViewport.Fov),
            Engine.MainViewport.AspectRatio,
            near, far);

        var frustum = new Frustum(Engine.MainViewport.GetViewMatrix() * projection);

        var center = frustum.Center;

        var radius = 0f;

        foreach (var c in frustum.Corners)
        {
            radius = MathF.Max(radius, (c - center).Length);
        }

        radius = MathF.Ceiling(radius * 16f) / 16f; // quantize away float jitter

        var direction = Vector3.Transform(Vector3.UnitY, Rotation).Normalized();

        var up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;

        const float casterPadding = 2500f; // how far behind the slice casters can live
        var backOff = radius + casterPadding;

        var lightView = Matrix4.LookAt(direction * backOff, Vector3.Zero, up);

        var texel = radius * 2f / ShadowResolution;
        var centerLs = Vector3.TransformPosition(center, lightView);

        var x = MathF.Floor(centerLs.X / texel) * texel;
        var y = MathF.Floor(centerLs.Y / texel) * texel;

        return lightView * Matrix4.CreateOrthographicOffCenter(
            x - radius, x + radius,
            y - radius, y + radius,
            -centerLs.Z - radius - casterPadding,   // near
            -centerLs.Z + radius);                  // far
    }
}