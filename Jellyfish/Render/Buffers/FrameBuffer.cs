using System;
using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class FrameBuffer : IDisposable
{
    public readonly int Handle = GL.CreateFramebuffer();

    public void Bind(FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        GL.BindFramebuffer(target, Handle);
    }

    public void Unbind(FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        GL.BindFramebuffer(target, 0);
    }

    public void AttachTexture(int textureHandle, FramebufferAttachment attachment = FramebufferAttachment.ColorAttachment0, int level = 0, int? layer = null)
    {
        if (layer == null)
        {
            GL.NamedFramebufferTexture(Handle, attachment, textureHandle, level);
        }
        else
        {
            GL.NamedFramebufferTextureLayer(Handle, attachment, textureHandle, level, layer.Value);
        }
    }

    public void DrawInto(ColorBuffer buffer)
    {
        GL.NamedFramebufferDrawBuffer(Handle, buffer);
    }

    public void ReadFrom(ColorBuffer buffer)
    {
        GL.NamedFramebufferReadBuffer(Handle, buffer);
    }

    public bool Check()
    {
        var code = GL.CheckNamedFramebufferStatus(Handle, FramebufferTarget.Framebuffer);
        if (code != FramebufferStatus.FramebufferComplete)
        {
            Log.Context(this).Error("Framebuffer {Id} status check failed with code {Code}", Handle, code);
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        GL.DeleteFramebuffer(Handle);
    }
}