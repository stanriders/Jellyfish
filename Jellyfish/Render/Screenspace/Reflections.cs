using Jellyfish.Console;
using Jellyfish.Render.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Screenspace;

public partial class Reflections : ScreenspaceEffect
{
    [ConVar("mat_sslr_enabled", true)]
    private partial bool Enabled { get; set; }

    public Reflections() : base("Reflections", SizedInternalFormat.Rgba16f, new ScreenspaceReflections())
    {
        Priority = 0;
    }

    public override void Draw()
    {
        if (!Enabled)
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
public class ReflectionsBlurX : ScreenspaceEffect
{
    public ReflectionsBlurX() : base("ReflectionsBlurX", SizedInternalFormat.Rgba16f, new Blur("_rt_Reflections", Blur.Direction.Horizontal, Blur.Size.Blur9Slow))
    {
        Priority = 1;
    }
}

public class ReflectionsBlurY : ScreenspaceEffect
{
    public ReflectionsBlurY() : base("ReflectionsBlurY", SizedInternalFormat.Rgba16f, new Blur("_rt_ReflectionsBlurX", Blur.Direction.Vertical, Blur.Size.Blur9Slow))
    {
        Priority = 2;
    }
}