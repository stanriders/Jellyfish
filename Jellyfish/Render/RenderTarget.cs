using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;

namespace Jellyfish.Render;

public class RenderTarget
{
    public readonly int TextureHandle;
    public readonly Vector2 Size;
    public readonly int Levels;
    private readonly Texture _texture;

    public RenderTarget(string name, int width, int heigth, SizedInternalFormat internalFormat, FramebufferAttachment attachment, TextureWrapMode wrapMode, float[]? borderColor = null, bool enableCompare = false, int levels = 1, TextureMinFilter filtering = TextureMinFilter.Nearest)
    {
        Size = new Vector2(width, heigth);
        Levels = Math.Clamp(Math.Min(width, heigth) / 64, 1, levels);

        _texture = Engine.TextureManager.GetTexture(name, TextureTarget.Texture2d, false).Texture;
        TextureHandle = _texture.Handle;
        GL.BindTexture(TextureTarget.Texture2d, TextureHandle);

        GL.TextureStorage2D(TextureHandle, Levels, internalFormat, width, heigth);
        GL.TextureParameteri(TextureHandle, TextureParameterName.TextureMinFilter, new[] { (int)filtering });
        GL.TextureParameteri(TextureHandle, TextureParameterName.TextureMagFilter, new[] { (int)filtering });
        GL.TextureParameteri(TextureHandle, TextureParameterName.TextureWrapS, new[] { (int)wrapMode });
        GL.TextureParameteri(TextureHandle, TextureParameterName.TextureWrapT, new[] { (int)wrapMode });

        if (enableCompare)
        {
            GL.TextureParameteri(TextureHandle, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
            GL.TextureParameteri(TextureHandle, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
        }

        if (borderColor != null)
        {
            GL.TextureParameterf(TextureHandle, TextureParameterName.TextureBorderColor, borderColor);
        }

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2d, TextureHandle, 0);

        GL.BindTexture(TextureTarget.Texture2d, 0);

        _texture.Levels = Levels;
        _texture.Format = internalFormat.ToString();
    }

    public void Bind(uint unit)
    {
        GL.BindTextureUnit(unit, TextureHandle);
    }

    public void Unload()
    {
        Engine.TextureManager.RemoveTexture(_texture);
    }
}