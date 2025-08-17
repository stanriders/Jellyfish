using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;

namespace Jellyfish.Render.Shaders;

public class PostProcessing : Shader
{
    private readonly Texture _rtColor;
    private readonly Texture _rtAmbientOcclusion;

    private static float sceneExposure = 1.0f;
    private const float adj_speed = 0.05f;

    public bool IsEnabled { get; set; } = true;

    public PostProcessing() : 
        base("shaders/Screenspace.vert", null, "shaders/PostProcessing.frag")
    {
        _rtColor = TextureManager.GetTexture("_rt_Color")!;
        _rtAmbientOcclusion = TextureManager.GetTexture("_rt_Gtao")!;
    }

    public override void Bind()
    {
        base.Bind();

        _rtColor.Bind(0);
        _rtAmbientOcclusion.Bind(1);

        SetVector2("screenSize", new Vector2(MainWindow.WindowWidth, MainWindow.WindowHeight));
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
                sceneExposure = float.Lerp(sceneExposure, 0.5f / luminance * 0.5f, adj_speed);
                sceneExposure = Math.Clamp(sceneExposure, 0.1f, 1f);
            }

            SetFloat("exposure", sceneExposure);
        }
    }

    public override void Unbind()
    {
        GL.BindTextureUnit(0, 0);
        GL.BindTextureUnit(1, 0);

        base.Unbind();
    }

    public override void Unload()
    {
        _rtColor.Unload();
        _rtAmbientOcclusion.Unload();

        base.Unload();
    }
}