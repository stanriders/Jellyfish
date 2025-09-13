using ImageMagick;
using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;
using System;
using System.IO;

namespace Jellyfish.Render;

public class RenderTargetParams
{
    public required int Width { get; set; }
    public required int Heigth { get; set; }
    public required SizedInternalFormat InternalFormat { get; set; } // todo? this can probably be moved to the main TextureParams
    public required FramebufferAttachment Attachment { get; set; }
    public bool EnableCompare { get; set; } = false;
}

public class TextureParams
{
    public required string Name { get; set; }
    public TextureTarget Type { get; set; } = TextureTarget.Texture2d;
    public bool Srgb { get; set; } = false;
    public RenderTargetParams? RenderTargetParams { get; set; }
    public float[]? BorderColor { get; set; } = null;
    public int? MaxLevels { get; set; } = 1;
    public TextureMinFilter MinFiltering { get; set; } = TextureMinFilter.LinearMipmapLinear;
    public TextureMagFilter MagFiltering { get; set; } = TextureMagFilter.Linear;
    public TextureWrapMode WrapMode { get; set; } = TextureWrapMode.Repeat;
}

public class Texture
{
    public TextureParams Params { get; }
    public int Handle { get; }
    public int References { get; set; } = 1;
    public int Levels { get; private set; }
    public string Format { get; private set; } = string.Empty;

    private readonly bool _isError;

    public const string error_texture = "materials/error.png";

    public Texture(TextureParams textureParams)
    {
        Params = textureParams;

        if (string.IsNullOrEmpty(Params.Name))
            return;

        Handle = GL.CreateTexture(Params.Type);

        GL.ObjectLabel(ObjectIdentifier.Texture, (uint)Handle, Params.Name.Length, Params.Name);

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int)textureParams.MinFiltering);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int)textureParams.MagFiltering);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int)textureParams.WrapMode);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int)textureParams.WrapMode);

        if (textureParams.BorderColor != null)
        {
            GL.TextureParameterf(Handle, TextureParameterName.TextureBorderColor, textureParams.BorderColor);
        }

        // procedural textures create themselves
        if (Params.Name.StartsWith("_"))
        {
            CreateRenderTarget();
            return;
        }

        Params.MaxLevels = 8;

        if (!File.Exists(Params.Name))
        {
            Log.Context(this).Warning("Texture {Path} doesn't exist!", Params.Name);
            Params.Name = error_texture;
        }

        if (Params.Name == error_texture)
            _isError = true;

        using var image = new MagickImage(Params.Name);

        // downsample sRGB-expected textures since ogl doesn't support 16-bit sRGB
        if (image.Depth == 16 && Params.Srgb)
        {
            image.Depth = 8;
        }

        using var data = image.GetPixelsUnsafe(); // feels scary

        var hasAlpha = image.ChannelCount == 4;

        var pixelFormat = hasAlpha ? PixelFormat.Rgba : PixelFormat.Rgb;
        var internalPixelFormat = hasAlpha ?
            Params.Srgb ? SizedInternalFormat.Srgb8Alpha8 : SizedInternalFormat.Rgba8 :
            Params.Srgb ? SizedInternalFormat.Srgb8 : SizedInternalFormat.Rgb8;

        if (image.Depth == 16)
        {
            internalPixelFormat = hasAlpha ? SizedInternalFormat.Rgba16 : SizedInternalFormat.Rgb16;
        }

        var levels = Math.Clamp(Math.Min((int)image.Width, (int)image.Height) / 16, 1, Params.MaxLevels.Value);

        GL.TextureStorage2D(Handle, levels, internalPixelFormat, (int)image.Width, (int)image.Height);
        GL.TextureSubImage2D(Handle, 0, 0, 0, (int)image.Width, (int)image.Height, pixelFormat, PixelType.UnsignedByte,
            data.GetAreaPointer(0, 0, image.Width, image.Height));

        GL.GenerateTextureMipmap(Handle);

        Levels = levels;
        Format = internalPixelFormat.ToString();
    }

    private void CreateRenderTarget()
    {
        if (Params.RenderTargetParams == null)
            return;

        if (Params.MaxLevels != null)
            Levels = Math.Clamp(Math.Min(Params.RenderTargetParams.Width, Params.RenderTargetParams.Heigth) / 64, 1, Params.MaxLevels.Value);
        else
            Levels = MaxLevels(Params.RenderTargetParams.Width, Params.RenderTargetParams.Heigth);

        GL.TextureStorage2D(Handle, Levels, Params.RenderTargetParams.InternalFormat, Params.RenderTargetParams.Width, Params.RenderTargetParams.Heigth);

        if (Params.RenderTargetParams.EnableCompare)
        {
            GL.TextureParameteri(Handle, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
            GL.TextureParameteri(Handle, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
        }

        // other types should bind manually
        if (Params.Type == TextureTarget.Texture2d)
        {
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, Params.RenderTargetParams.Attachment, Params.Type, Handle, 0);
        }

        GL.BindTexture(Params.Type, 0);

        Format = Params.RenderTargetParams.InternalFormat.ToString();
    }

    public void Bind(uint unit)
    {
        if (Handle != 0)
        {
            GL.BindTextureUnit(unit, Handle);
        }
    }

    public void Unload()
    {
        if (!_isError && Handle != 0)
            Engine.TextureManager.RemoveTexture(this);
    }

    public override string ToString() => Params.Name;

    private int MaxLevels(int width, int height)
    {
        var maxDim = Math.Max(width, height);
        return (int)Math.Floor(Math.Log(maxDim, 2)) + 1;
    }
}