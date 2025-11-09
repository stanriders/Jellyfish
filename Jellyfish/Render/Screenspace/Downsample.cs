using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Screenspace;

public class Downsample : ScreenspaceEffect
{
    public Downsample() : base(new TextureParams
    {
        Name = "_rt_Downsample",
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
    }, new Shaders.Downsample("_rt_Color"))
    {
        Priority = 0;
    }

    public override void Draw()
    {
        GL.Viewport(0, 0, Engine.MainViewport.Size.X / 2, Engine.MainViewport.Size.Y / 2);
        base.Draw();
        GL.Viewport(0, 0, Engine.MainViewport.Size.X, Engine.MainViewport.Size.Y);
    }
}
public class Downsample4 : ScreenspaceEffect
{
    public Downsample4() : base(new TextureParams
    {
        Name = "_rt_Downsample4",
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
    }, new Shaders.Downsample("_rt_Downsample"))
    {
        Priority = 1;
    }

    public override void Draw()
    {
        GL.Viewport(0, 0, Engine.MainViewport.Size.X / 4, Engine.MainViewport.Size.Y / 4);
        base.Draw();
        GL.Viewport(0, 0, Engine.MainViewport.Size.X, Engine.MainViewport.Size.Y);
    }
}
public class Downsample8 : ScreenspaceEffect
{
    public Downsample8() : base(new TextureParams
    {
        Name = "_rt_Downsample8",
        WrapMode = TextureWrapMode.ClampToEdge,
        MinFiltering = TextureMinFilter.Linear,
        MagFiltering = TextureMagFilter.Linear,
        RenderTargetParams = new RenderTargetParams
        {
            Width = Engine.MainViewport.Size.X / 8,
            Heigth = Engine.MainViewport.Size.Y / 8,
            InternalFormat = SizedInternalFormat.Rgb16f,
            Attachment = FramebufferAttachment.ColorAttachment0,
        }
    }, new Shaders.Downsample("_rt_Downsample4"))
    {
        Priority = 2;
    }

    public override void Draw()
    {
        GL.Viewport(0, 0, Engine.MainViewport.Size.X / 8, Engine.MainViewport.Size.Y / 8);
        base.Draw();
        GL.Viewport(0, 0, Engine.MainViewport.Size.X, Engine.MainViewport.Size.Y);
    }
}