using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public class RenderTarget
{
    public readonly int TextureHandle;
    public readonly Vector2 Size;

    public RenderTarget(string name, int width, int heigth, PixelFormat format, FramebufferAttachment attachment, PixelType pixelType, TextureWrapMode wrapMode, float[]? borderColor = null)
    {
        Size = new Vector2(width, heigth);

        TextureHandle = TextureManager.GenerateHandle(name);
        GL.BindTexture(TextureTarget.Texture2D, TextureHandle);

        GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)format, width, heigth, 0, format, pixelType, IntPtr.Zero);
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Nearest });
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, new[] { (int)TextureMinFilter.Nearest });
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, new[] { (int)wrapMode });
        GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, new[] { (int)wrapMode });

        if (borderColor != null)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor,
                new[] { 1.0f, 1.0f, 1.0f, 1.0f });
        }

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, TextureHandle, 0);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2D, TextureHandle);
    }
}