using Jellyfish.Render.Buffers;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Screenspace;

public abstract class ScreenspaceEffect
{
    protected readonly FrameBuffer Buffer;
    protected readonly Texture RenderTarget;
    protected readonly Shader Shader;

    protected Color4<Rgba> ClearColor { get; set; } = new Color4<Rgba>(1.0f, 1.0f, 1.0f, 1.0f);
    public int Priority { get; protected set; } = int.MaxValue;

    protected ScreenspaceEffect(string rtName, SizedInternalFormat format, Shader shader)
    {
        Shader = shader;

        RenderTarget = Engine.TextureManager.CreateTexture(new RenderTargetParams
        {
            Width = Engine.MainViewport.Size.X,
            Heigth = Engine.MainViewport.Size.Y,
            TextureParams = new TextureParams
            {
                Name = $"_rt_{rtName}",
                WrapMode = TextureWrapMode.ClampToEdge,
                MinFiltering = TextureMinFilter.Nearest,
                MagFiltering = TextureMagFilter.Nearest,
                InternalFormat = format
            }
        });

        Buffer = new FrameBuffer();
        Buffer.AttachTexture(RenderTarget.Handle, attachment: FramebufferAttachment.ColorAttachment0);
        Buffer.DrawInto(ColorBuffer.ColorAttachment0);
        Buffer.Check();
    }

    protected ScreenspaceEffect(RenderTargetParams rtParams, Shader shader)
    {
        Shader = shader;

        RenderTarget = Engine.TextureManager.CreateTexture(rtParams);

        Buffer = new FrameBuffer();
        Buffer.AttachTexture(RenderTarget.Handle, attachment: FramebufferAttachment.ColorAttachment0);
        Buffer.DrawInto(ColorBuffer.ColorAttachment0);
        Buffer.Check();
    }

    public virtual void Draw()
    {
        Buffer.Bind(FramebufferTarget.DrawFramebuffer);

        GL.ClearColor(ClearColor);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        Shader.Bind();
        CommonShapes.DrawQuad();
        Shader.Unbind();

        Buffer.Unbind();
    }

    public virtual void Unload()
    {
        Buffer.Dispose();
        RenderTarget.Unload();
        Shader.Unload();
    }
}