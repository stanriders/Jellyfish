using Jellyfish.Console;
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Screenspace;

public class BloomEnabled() : ConVar<bool>("mat_bloom_enabled", true);
public class BloomThreshold() : ConVar<float>("mat_bloom_threshold", 1.0f);
public class BloomStrength() : ConVar<float>("mat_bloom_strength", 0.2f);
public class Bloom : ScreenspaceEffect
{
    public Bloom() : base("Bloom", SizedInternalFormat.Rgb16f, new Shaders.Bloom())
    {
    }
    public override void Draw()
    {
        if (!ConVarStorage.Get<bool>("mat_bloom_enabled"))
        {
            Buffer.Bind(FramebufferTarget.DrawFramebuffer);

            GL.ClearColor(Color4.Black);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            Buffer.Unbind();
            return;
        }

        base.Draw();
    }
}
public class BloomBlurX : ScreenspaceEffect
{
    public BloomBlurX() : base("BloomBlurX", SizedInternalFormat.Rgb16f, new Blur("_rt_Bloom", Blur.Direction.Horizontal, Blur.Size.Blur13Slow))
    {
    }
}

public class BloomBlurY : ScreenspaceEffect
{
    public BloomBlurY() : base("BloomBlurY", SizedInternalFormat.Rgb16f, new Blur("_rt_BloomBlurX", Blur.Direction.Vertical, Blur.Size.Blur13Slow))
    {
    }
}