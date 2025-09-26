using Jellyfish.Console;
using Jellyfish.Render.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Screenspace;

public class SslrEnabled() : ConVar<bool>("mat_sslr_enabled", true);
public class Reflections : ScreenspaceEffect
{
    public Reflections() : base("Reflections", SizedInternalFormat.Rgba16f, new ScreenspaceReflections())
    {
    }
    public override void Draw()
    {
        if (!ConVarStorage.Get<bool>("mat_sslr_enabled"))
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
    public ReflectionsBlurX() : base("ReflectionsBlurX", SizedInternalFormat.Rgba16f, new Blur("_rt_Reflections", Blur.Direction.Horizontal, Blur.Size.Blur9))
    {
    }
}

public class ReflectionsBlurY : ScreenspaceEffect
{
    public ReflectionsBlurY() : base("ReflectionsBlurY", SizedInternalFormat.Rgba16f, new Blur("_rt_ReflectionsBlurX", Blur.Direction.Vertical, Blur.Size.Blur9))
    {
    }
}