using Jellyfish.Console;
using Jellyfish.Render.Shaders.SMAA;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Screenspace;

public class SMAAEnabled() : ConVar<bool>("mat_smaa_enabled", true);

public class SMAAEdgeDetection : ScreenspaceEffect
{
    public SMAAEdgeDetection() : base(new TextureParams
    {
        Name = "_rt_SMAAEdgeDetection",
        WrapMode = TextureWrapMode.ClampToEdge,
        MinFiltering = TextureMinFilter.Linear,
        MagFiltering = TextureMagFilter.Linear,
        InternalFormat = SizedInternalFormat.Rgba8,
        RenderTargetParams = new RenderTargetParams
        {
            Width = Engine.MainViewport.Size.X,
            Heigth = Engine.MainViewport.Size.Y,
            Attachment = FramebufferAttachment.ColorAttachment0,
        }
    }, new EdgeDetection())
    {
        ClearColor = new Color4<Rgba>(0, 0, 0, 1);
        Priority = 101; // must be after the Combine
    }

    public override void Draw()
    {
        if (!ConVarStorage.Get<bool>("mat_smaa_enabled"))
        {
            Buffer.Bind(FramebufferTarget.DrawFramebuffer);

            GL.ClearColor(ClearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            Buffer.Unbind();
            return;
        }

        base.Draw();
    }
}

public class SMAABlendingWeightCalculation : ScreenspaceEffect
{
    public SMAABlendingWeightCalculation() : base(new TextureParams
    {
        Name = "_rt_SMAABlendingWeightCalculation",
        WrapMode = TextureWrapMode.ClampToEdge,
        MinFiltering = TextureMinFilter.Linear,
        MagFiltering = TextureMagFilter.Linear,
        InternalFormat = SizedInternalFormat.Rgba8,
        RenderTargetParams = new RenderTargetParams
        {
            Width = Engine.MainViewport.Size.X,
            Heigth = Engine.MainViewport.Size.Y,
            Attachment = FramebufferAttachment.ColorAttachment0,
        }
    }, new BlendingWeightCalculation())
    {
        ClearColor = new Color4<Rgba>(0, 0, 0, 1);
        Priority = 102;
    }

    public override void Draw()
    {
        if (!ConVarStorage.Get<bool>("mat_smaa_enabled"))
        {
            Buffer.Bind(FramebufferTarget.DrawFramebuffer);

            GL.ClearColor(ClearColor);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            Buffer.Unbind();
            return;
        }

        base.Draw();
    }
}

public class SMAANeighborhoodBlending : ScreenspaceEffect
{
    public SMAANeighborhoodBlending() : base(new TextureParams
    {
        Name = "_rt_SMAANeighborhoodBlending",
        WrapMode = TextureWrapMode.ClampToEdge,
        MinFiltering = TextureMinFilter.Linear,
        MagFiltering = TextureMagFilter.Linear,
        InternalFormat = SizedInternalFormat.Rgb8,
        RenderTargetParams = new RenderTargetParams
        {
            Width = Engine.MainViewport.Size.X,
            Heigth = Engine.MainViewport.Size.Y,
            Attachment = FramebufferAttachment.ColorAttachment0,
        }
    }, new NeighborhoodBlending())
    {
        ClearColor = new Color4<Rgba>(0, 0, 0, 1);
        Priority = 103;
    }
}