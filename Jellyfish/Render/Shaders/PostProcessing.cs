using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace Jellyfish.Render.Shaders;

public class PostprocessingEnabled() : ConVar<bool>("mat_postprocess_enabled", true, Keys.P);
public class PostProcessing : Shader
{
    private readonly Texture _rtColor;
    private readonly Texture _rtAmbientOcclusion;
    private readonly Texture _rtBloom;

    private static float sceneExposure = 1.0f;
    private const float adj_speed = 0.035f;

    private bool _ranPreviousFrame;

    public PostProcessing() : 
        base("shaders/Screenspace.vert", null, "shaders/PostProcessing.frag")
    {
        _rtColor = Engine.TextureManager.GetTexture("_rt_Color")!;
        _rtAmbientOcclusion = Engine.TextureManager.GetTexture("_rt_GtaoBlurY")!;
        _rtBloom = Engine.TextureManager.GetTexture("_rt_Bloom")!;
    }

    public override void Bind()
    {
        base.Bind();

        BindTexture(0, _rtColor);
        BindTexture(1, _rtAmbientOcclusion);
        BindTexture(2, _rtBloom);

        var isEnabled = ConVarStorage.Get<bool>("mat_postprocess_enabled");

        SetInt("isEnabled", isEnabled ? 1 : 0);
        SetFloat("bloomStrength", ConVarStorage.Get<float>("mat_bloom_strength"));

        if (isEnabled)
        {
            if (!_ranPreviousFrame)
            {
                GL.GenerateTextureMipmap(_rtColor
                    .Handle); // TODO: This generates mipmaps every frame, replace with a histogram calculation

                var pixel = new float[3];
                GL.GetTextureSubImage(_rtColor.Handle,
                    _rtColor.Levels - 1,
                    0, 0, 0,
                    1, 1, 1,
                    PixelFormat.Rgb, PixelType.Float,
                    pixel.Length * sizeof(float), pixel);

                var luminance = 0.2126f * pixel[0] + 
                                0.7152f * pixel[1] + 
                                0.0722f * pixel[2]; // Calculate a weighted average

                luminance = Math.Max(luminance, 0.00001f);

                if (!double.IsNaN(luminance))
                {
                    const float key = 0.14f;
                    var targetExposure = key / luminance;

                    sceneExposure = float.Lerp(sceneExposure, targetExposure, adj_speed);
                    sceneExposure = Math.Clamp(sceneExposure, 0.01f, 8.0f);
                }

                SetFloat("exposure", sceneExposure);
                SetInt("toneMappingMode", 2);
                _ranPreviousFrame = true;
            }
            else
            {
                _ranPreviousFrame = false;
            }
        }
    }

    public override void Unload()
    {
        _rtColor.Unload();
        _rtAmbientOcclusion.Unload();
        _rtBloom.Unload();

        base.Unload();
    }
}