using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class FrameBuffer
{
    public readonly int Handle;

    public FrameBuffer()
    {
        Handle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Bind(FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        GL.BindFramebuffer(target, Handle);
    }

    public void Unbind(FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        GL.BindFramebuffer(target, 0);
    }

    public void Unload()
    {
        GL.DeleteFramebuffer(Handle);
    }

    public bool Check()
    {
        var code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (code != FramebufferStatus.FramebufferComplete)
        {
            Log.Context(this).Error("Framebuffer {Id} status check failed with code {Code}", Handle, code);
            return false;
        }

        return true;
    }
}