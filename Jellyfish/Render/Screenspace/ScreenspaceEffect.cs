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

        Buffer = new FrameBuffer();
        Buffer.Bind();

        RenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = $"_rt_{rtName}",
            WrapMode = TextureWrapMode.ClampToEdge,
            MinFiltering = TextureMinFilter.Nearest,
            MagFiltering = TextureMagFilter.Nearest,
            InternalFormat = format,
            RenderTargetParams = new RenderTargetParams
            {
                Width = Engine.MainViewport.Size.X,
                Heigth = Engine.MainViewport.Size.Y,
                Attachment = FramebufferAttachment.ColorAttachment0,
            }
        });

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        Buffer.Check();
        Buffer.Unbind();
    }

    protected ScreenspaceEffect(TextureParams textureParams, Shader shader)
    {
        Shader = shader;

        Buffer = new FrameBuffer();
        Buffer.Bind();

        RenderTarget = Engine.TextureManager.CreateTexture(textureParams);

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        Buffer.Check();
        Buffer.Unbind();
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
        Buffer.Unload();
        RenderTarget.Unload();
        Shader.Unload();
    }
}