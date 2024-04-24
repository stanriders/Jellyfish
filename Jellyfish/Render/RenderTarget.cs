using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public class RenderTarget
{
    public readonly int TextureHandle;
    public readonly Vector2 Size;

    public RenderTarget(int width, int heigth, PixelFormat format, FramebufferAttachment attachment, PixelType pixelType)
    {
        Size = new Vector2(width, heigth);

        TextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, TextureHandle);

        GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)format, width, heigth, 0, format, pixelType, IntPtr.Zero);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Linear });
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMinFilter.Linear });
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new[] { (int)TextureWrapMode.Repeat });
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new[] { (int)TextureWrapMode.Repeat });
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, TextureHandle, 0);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2D, TextureHandle);
    }
}