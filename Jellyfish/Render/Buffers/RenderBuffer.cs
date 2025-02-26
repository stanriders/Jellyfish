using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public static class RenderBuffer
{
    public static void Create(InternalFormat type, FramebufferAttachment attachment, int width, int heigth)
    {
        var renderBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, type, width, heigth);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, renderBuffer);
    }
}