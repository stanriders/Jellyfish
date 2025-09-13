using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class RenderBuffer
{
    public readonly InternalFormat Type;
    public readonly int Handle;

    public static int Create(InternalFormat type, FramebufferAttachment attachment, int width, int heigth)
    {
        var renderBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, type, width, heigth);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, renderBuffer);

        return renderBuffer;
    }

    public RenderBuffer(InternalFormat type, FramebufferAttachment attachment, int width, int heigth)
    {
        Type = type;
        Handle = Create(type, attachment, width, heigth);
    }

    public void Bind()
    {
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Handle);
    }

    public void UpdateSize(int width, int heigth)
    {
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, Type, width, heigth);
    }
}