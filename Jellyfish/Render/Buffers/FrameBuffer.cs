using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class FrameBuffer
{
    private readonly int _framebufferHandle;

    public FrameBuffer()
    {
        _framebufferHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferHandle);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public void Bind(FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        GL.BindFramebuffer(target, _framebufferHandle);
    }

    public void Unbind(FramebufferTarget target = FramebufferTarget.Framebuffer)
    {
        GL.BindFramebuffer(target, 0);
    }

    public void Unload()
    {
        GL.DeleteFramebuffer(_framebufferHandle);
    }

    public bool Check()
    {
        var code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (code != FramebufferErrorCode.FramebufferComplete)
        {
            Log.Context(this).Error("Framebuffer {Id} status check failed with code {Code}", _framebufferHandle, code);
            return false;
        }

        return true;
    }
}