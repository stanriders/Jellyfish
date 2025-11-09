using Jellyfish.Console;
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Screenspace;

public class BloomEnabled() : ConVar<bool>("mat_bloom_enabled", true);
public class BloomStrength() : ConVar<float>("mat_bloom_strength", 0.04f);

public class Upsample4 : ScreenspaceEffect
{
    public Upsample4() : base(new TextureParams
    {
        Name = "_rt_Upsample4",
        WrapMode = TextureWrapMode.ClampToEdge,
        MinFiltering = TextureMinFilter.Linear,
        MagFiltering = TextureMagFilter.Linear,
        RenderTargetParams = new RenderTargetParams
        {
            Width = Engine.MainViewport.Size.X / 4,
            Heigth = Engine.MainViewport.Size.Y / 4,
            InternalFormat = SizedInternalFormat.Rgb16f,
            Attachment = FramebufferAttachment.ColorAttachment0,
        }
    }, new Shaders.Bloom("_rt_Downsample8", 1f))
    {
        Priority = 10;
    }
    public override void Draw()
    {
        GL.Viewport(0, 0, Engine.MainViewport.Size.X / 4, Engine.MainViewport.Size.Y / 4);
        base.Draw();
        GL.Viewport(0, 0, Engine.MainViewport.Size.X, Engine.MainViewport.Size.Y);
    }
}
public class Upsample2 : ScreenspaceEffect
{
    public Upsample2() : base(new TextureParams
    {
        Name = "_rt_Upsample2",
        WrapMode = TextureWrapMode.ClampToEdge,
        MinFiltering = TextureMinFilter.Linear,
        MagFiltering = TextureMagFilter.Linear,
        RenderTargetParams = new RenderTargetParams
        {
            Width = Engine.MainViewport.Size.X / 2,
            Heigth = Engine.MainViewport.Size.Y / 2,
            InternalFormat = SizedInternalFormat.Rgb16f,
            Attachment = FramebufferAttachment.ColorAttachment0,
        }
    }, new Shaders.Bloom("_rt_Upsample4", 0.1f))
    {
        Priority = 11;
    }
    public override void Draw()
    {
        GL.Viewport(0, 0, Engine.MainViewport.Size.X / 2, Engine.MainViewport.Size.Y / 2);
        base.Draw();
        GL.Viewport(0, 0, Engine.MainViewport.Size.X, Engine.MainViewport.Size.Y);
    }
}
public class Bloom : ScreenspaceEffect
{
    public Bloom() : base(new TextureParams
    {
        Name = "_rt_Bloom",
        WrapMode = TextureWrapMode.ClampToEdge,
        MinFiltering = TextureMinFilter.Linear,
        MagFiltering = TextureMagFilter.Linear,
        RenderTargetParams = new RenderTargetParams
        {
            Width = Engine.MainViewport.Size.X,
            Heigth = Engine.MainViewport.Size.Y,
            InternalFormat = SizedInternalFormat.Rgb16f,
            Attachment = FramebufferAttachment.ColorAttachment0,
        }
    }, new Shaders.Bloom("_rt_Upsample2", 0.01f))
    {
        Priority = 12;
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