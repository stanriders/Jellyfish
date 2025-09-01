using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;

namespace Jellyfish.Render;

public class RenderTargetParams
{
    public required string Name { get; set; }
    public required int Width { get; set; }
    public required int Heigth { get; set; }
    public required SizedInternalFormat InternalFormat { get; set; }
    public required FramebufferAttachment Attachment { get; set; }
    public required TextureWrapMode WrapMode { get; set; }
    public float[]? BorderColor { get; set; } = null;
    public bool EnableCompare { get; set; } = false;
    public int Levels { get; set; } = 1;
    public TextureMinFilter Filtering { get; set; } = TextureMinFilter.Nearest;
    public TextureTarget TextureType { get; set; } = TextureTarget.Texture2d;
}

public class RenderTarget
{
    public readonly int TextureHandle;
    public readonly int ClampedLevels;
    public readonly RenderTargetParams Params;
    private readonly Texture _texture;

    public RenderTarget(RenderTargetParams rtParams)
    {
        Params = rtParams;
        ClampedLevels = Math.Clamp(Math.Min(rtParams.Width, rtParams.Heigth) / 64, 1, rtParams.Levels);

        _texture = Engine.TextureManager.GetTexture(rtParams.Name, rtParams.TextureType, false).Texture;
        TextureHandle = _texture.Handle;
        GL.BindTexture(rtParams.TextureType, TextureHandle);

        GL.TextureStorage2D(TextureHandle, ClampedLevels, rtParams.InternalFormat, rtParams.Width, rtParams.Heigth);
        GL.TextureParameteri(TextureHandle, TextureParameterName.TextureMinFilter, new[] { (int)rtParams.Filtering });
        GL.TextureParameteri(TextureHandle, TextureParameterName.TextureMagFilter, new[] { (int)rtParams.Filtering });
        GL.TextureParameteri(TextureHandle, TextureParameterName.TextureWrapS, new[] { (int)rtParams.WrapMode });
        GL.TextureParameteri(TextureHandle, TextureParameterName.TextureWrapT, new[] { (int)rtParams.WrapMode });

        if (rtParams.EnableCompare)
        {
            GL.TextureParameteri(TextureHandle, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
            GL.TextureParameteri(TextureHandle, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
        }

        if (rtParams.BorderColor != null)
        {
            GL.TextureParameterf(TextureHandle, TextureParameterName.TextureBorderColor, rtParams.BorderColor);
        }

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, rtParams.Attachment, rtParams.TextureType, TextureHandle, 0);

        GL.BindTexture(rtParams.TextureType, 0);

        _texture.Levels = ClampedLevels;
        _texture.Format = rtParams.InternalFormat.ToString();
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