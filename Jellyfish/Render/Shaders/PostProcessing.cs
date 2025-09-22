using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;

namespace Jellyfish.Render.Shaders;

public class PostProcessing : Shader
{
    private readonly Texture _rtColor;
    private readonly Texture _rtAmbientOcclusion;

    private static float sceneExposure = 1.0f;
    private const float adj_speed = 0.035f;

    public bool IsEnabled { get; set; } = true;

    public PostProcessing() : 
        base("shaders/Screenspace.vert", null, "shaders/PostProcessing.frag")
    {
        _rtColor = Engine.TextureManager.GetTexture("_rt_Color")!;
        _rtAmbientOcclusion = Engine.TextureManager.GetTexture("_rt_GtaoBlurY")!;
    }

    public override void Bind()
    {
        base.Bind();

        BindTexture(0, _rtColor);
        BindTexture(1, _rtAmbientOcclusion);

        SetVector2("screenSize", new Vector2(Engine.MainViewport.Size.X, Engine.MainViewport.Size.Y));
        SetInt("isEnabled", IsEnabled ? 1 : 0);

        if (IsEnabled)
        {
            GL.GenerateTextureMipmap(_rtColor.Handle); // TODO: This generates mipmaps every frame, replace with a histogram calculation

            var pixel = new float[3];
            GL.GetTextureSubImage(_rtColor.Handle,
                _rtColor.Levels - 1,
                0, 0, 0,
                1, 1, 1,
                PixelFormat.Rgb, PixelType.Float,
                pixel.Length * sizeof(float), pixel);

            var luminance = 0.2126f * pixel[0] + 0.7152f * pixel[1] + 0.0722f * pixel[2]; // Calculate a weighted average
            luminance = Math.Max(luminance, 0.00001f);

            if (!double.IsNaN(luminance))
            {
                const float key = 0.12f;
                var targetExposure = key / luminance;

                sceneExposure = float.Lerp(sceneExposure, targetExposure, adj_speed);
                sceneExposure = Math.Clamp(sceneExposure, 0.03125f, 4.0f);
            }

            SetFloat("exposure", sceneExposure);
            SetInt("toneMappingMode", 3);
        }
    }

    public override void Unload()
    {
        _rtColor.Unload();
        _rtAmbientOcclusion.Unload();

        base.Unload();
    }
}