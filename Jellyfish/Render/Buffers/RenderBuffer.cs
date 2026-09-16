using System;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class RenderBuffer : IDisposable
{
    public readonly InternalFormat Type;
    public readonly int Handle;

    public RenderBuffer(InternalFormat type, FramebufferAttachment attachment, int width, int height)
    {
        Type = type;
        Handle = GL.GenRenderbuffer();

        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Handle);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, type, width, height);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, attachment, RenderbufferTarget.Renderbuffer, Handle);
    }

    public void Bind()
    {
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, Handle);
    }

    public void UpdateSize(int width, int heigth)
    {
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, Type, width, heigth);
    }

    public void Dispose()
    {
        GL.DeleteRenderbuffer(Handle);
    }
}