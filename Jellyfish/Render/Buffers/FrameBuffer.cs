using System;
using OpenTK.Graphics.OpenGL;
using Serilog;

namespace Jellyfish.Render.Buffers;

public class FrameBuffer
{
    private readonly int _framebufferHandle;
    public readonly int FramebufferTextureHandle;

    public FrameBuffer()
    {
        _framebufferHandle = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebufferHandle);

        FramebufferTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, FramebufferTextureHandle);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb,
            MainWindow.WindowWidth, MainWindow.WindowHeight, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Linear });
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMinFilter.Linear });
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new[] { (int)TextureWrapMode.Repeat });
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new[] { (int)TextureWrapMode.Repeat });
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, FramebufferTextureHandle, 0);

        var renderBuffer = GL.GenRenderbuffer();
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, renderBuffer);
        GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, MainWindow.WindowWidth, MainWindow.WindowHeight);
        GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

        GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, renderBuffer);
        
        var code = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (code != FramebufferErrorCode.FramebufferComplete)
        {
            Log.Error("[FrameBuffer] Framebuffer {Id} status check failed with code {Code}", _framebufferHandle, code);
        }
        
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
}