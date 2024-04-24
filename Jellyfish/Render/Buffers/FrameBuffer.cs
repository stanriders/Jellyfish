using OpenTK.Graphics.OpenGL;
using Serilog;

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

    public void Bind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferHandle);
    }

    public void Unbind()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    public bool Check()
    {
        var code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (code != FramebufferErrorCode.FramebufferComplete)
        {
            Log.Error("[FrameBuffer] Framebuffer {Id} status check failed with code {Code}", _framebufferHandle, code);
            return false;
        }

        return true;
    }
}