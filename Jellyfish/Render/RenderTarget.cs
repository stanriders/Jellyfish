using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public class RenderTarget
{
    public readonly int TextureHandle;
    public readonly Vector2 Size;
    private readonly Texture _texture;

    public RenderTarget(string name, int width, int heigth, PixelFormat format, FramebufferAttachment attachment, PixelType pixelType, TextureWrapMode wrapMode, float[]? borderColor = null)
    {
        Size = new Vector2(width, heigth);

        _texture = TextureManager.GetTexture(name, TextureTarget.Texture2d).Texture;
        TextureHandle = _texture.Handle;
        GL.BindTexture(TextureTarget.Texture2d, TextureHandle);

        GL.TexImage2D(TextureTarget.Texture2d, 0, (InternalFormat)format, width, heigth, 0, format, pixelType, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, new[] { (int)TextureMinFilter.Nearest });
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, new[] { (int)TextureMinFilter.Nearest });
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, new[] { (int)wrapMode });
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, new[] { (int)wrapMode });

        if (borderColor != null)
        {
            GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor,
                new[] { 1.0f, 1.0f, 1.0f, 1.0f });
        }

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2d, TextureHandle, 0);

        GL.BindTexture(TextureTarget.Texture2d, 0);
    }

    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2d, TextureHandle);
    }

    public void Unload()
    {
        TextureManager.RemoveTexture(_texture);
    }
}