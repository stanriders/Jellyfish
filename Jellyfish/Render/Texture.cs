using ImageMagick;
using Jellyfish.Console;
using Jellyfish.Debug;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.IO;

namespace Jellyfish.Render;

public class RenderTargetParams
{
    public required int Width { get; set; }
    public required int Heigth { get; set; }
    public required FramebufferAttachment? Attachment { get; set; }
    public bool EnableCompare { get; set; } = false;
    public required TextureParams TextureParams { get; set; }
}

public class TextureParams
{
    public string? Path { get; set; }
    public string? Name { get; set; }
    public TextureTarget Type { get; set; } = TextureTarget.Texture2D;
    public bool Srgb { get; set; } = false;
    public float[]? BorderColor { get; set; } = null;
    public int? MaxLevels { get; set; }
    public TextureMinFilter MinFiltering { get; set; } = TextureMinFilter.LinearMipmapLinear;
    public TextureMagFilter MagFiltering { get; set; } = TextureMagFilter.Linear;
    public TextureWrapMode WrapMode { get; set; } = TextureWrapMode.Repeat;
    public SizedInternalFormat? InternalFormat { get; set; }
    public PixelFormat? PixelFormat { get; set; }
}

public class InvalidTextureException(string message) : Exception(message);

public class Texture
{
    public RenderTargetParams? RenderTargetParams { get; }
    public TextureParams Params { get; }
    public int Handle { get; }
    public int References { get; set; } = 1;
    public int Levels { get; private set; }
    public string Format { get; private set; }
    public bool HasAlpha { get; private set; }
    public Vector2 Size { get; private set; }

    private bool _isDeleted;
    private readonly NativeMemoryMeasurement.NativeMemoryTracker? _memoryTracker;

    public Texture(TextureParams textureParams)
    {
        Params = textureParams;

        if (string.IsNullOrEmpty(Params.Name))
        {
            if (!string.IsNullOrEmpty(Params.Path))
            {
                Params.Name = Params.Path;
            }
            else
            {
                Log.Context(this).Warning("Trying to create a texture with null Name and Path");
                throw new InvalidTextureException("Trying to create a texture with null Name and Path");
            }
        }

        Handle = GL.CreateTexture(Params.Type);

        GL.ObjectLabel(ObjectIdentifier.Texture, Handle, Params.Name.Length, Params.Name);

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int)textureParams.MinFiltering);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int)textureParams.MagFiltering);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int)textureParams.WrapMode);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int)textureParams.WrapMode);

        if (textureParams.BorderColor != null)
        {
            GL.TextureParameterf(Handle, TextureParameterName.TextureBorderColor, textureParams.BorderColor);
        }

        var path = Params.Path ?? Params.Name;
        if (!File.Exists(path))
        {
            Log.Context(this).Warning("Texture {Path} doesn't exist!", Params.Name);
            throw new InvalidTextureException($"Texture {Params.Name} doesn't exist!");
        }

        using var image = new MagickImage(path);

        // downsample sRGB-expected textures since ogl doesn't support 16-bit sRGB
        if (image.Depth == 16 && Params.Srgb)
        {
            image.Depth = 8;
        }

        using var data = image.GetPixelsUnsafe(); // feels scary

        HasAlpha = image.ChannelCount == 4;

        var pixelFormat = Params.PixelFormat ?? (HasAlpha ? PixelFormat.Rgba : PixelFormat.Rgb);

        var internalPixelFormat = Params.InternalFormat ??
                                  (HasAlpha
                                      ? Params.Srgb ? SizedInternalFormat.Srgb8Alpha8 : SizedInternalFormat.Rgba8
                                      : Params.Srgb ? SizedInternalFormat.Srgb8 : SizedInternalFormat.Rgb8);

        if (image.Depth == 16)
        {
            internalPixelFormat = HasAlpha ? SizedInternalFormat.Rgba16 : SizedInternalFormat.Rgb16;
        }

        var maxLevels = Params.MaxLevels ?? 8;
        Levels = Math.Clamp(Math.Min((int)image.Width, (int)image.Height) / 16, 1, maxLevels);

        GL.TextureStorage2D(Handle, Levels, internalPixelFormat, (int)image.Width, (int)image.Height);
        GL.TextureSubImage2D(Handle, 0, 0, 0, (int)image.Width, (int)image.Height, pixelFormat, PixelType.UnsignedByte,
            data.GetAreaPointer(0, 0, image.Width, image.Height));

        _memoryTracker = NativeMemoryMeasurement.AddMemory(this, image.Width * image.Height * image.ChannelCount);

        if (Levels > 1)
            GL.GenerateTextureMipmap(Handle);

        Format = internalPixelFormat.ToString();
        Size = new Vector2(image.Width, image.Height);
    }

    public Texture(RenderTargetParams rtParams)
    {
        Params = rtParams.TextureParams;
        RenderTargetParams = rtParams;

        if (string.IsNullOrEmpty(Params.Name))
        {
            Log.Context(this).Warning("Trying to create a render target texture with null Name");
            throw new InvalidTextureException("Trying to create a render target texture with null Name");
        }

        Handle = GL.CreateTexture(Params.Type);

        GL.ObjectLabel(ObjectIdentifier.Texture, Handle, Params.Name.Length, Params.Name);

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int)Params.MinFiltering);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int)Params.MagFiltering);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int)Params.WrapMode);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int)Params.WrapMode);

        if (Params.BorderColor != null)
        {
            GL.TextureParameterf(Handle, TextureParameterName.TextureBorderColor, Params.BorderColor);
        }

        if (RenderTargetParams.EnableCompare)
        {
            GL.TextureParameteri(Handle, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.CompareRefToTexture);
            GL.TextureParameteri(Handle, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
        }

        var maxLevels = Params.MaxLevels ?? 1;

        if (maxLevels != -1)
            Levels = Math.Clamp(Math.Min(RenderTargetParams!.Width, RenderTargetParams.Heigth) / 64, 1, maxLevels);
        else
            Levels = MaxLevels(RenderTargetParams!.Width, RenderTargetParams.Heigth);

        GL.TextureStorage2D(Handle, Levels, Params.InternalFormat!.Value, RenderTargetParams.Width, RenderTargetParams.Heigth);

        _memoryTracker = NativeMemoryMeasurement.AddMemory(this, RenderTargetParams.Width * RenderTargetParams.Heigth * 4);

        // other types should bind manually
        if (Params.Type == TextureTarget.Texture2D && RenderTargetParams.Attachment != null)
        {
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, RenderTargetParams.Attachment.Value, Params.Type, Handle, 0);
        }

        Format = Params.InternalFormat.ToString()!;
        Size = new Vector2(RenderTargetParams.Width, RenderTargetParams.Heigth);
    }

    public void Bind(uint unit)
    {
        if (_isDeleted)
            throw new Exception("Trying to bind a deleted texture!");

        if (Handle != 0)
        {
            GL.BindTextureUnit(unit, Handle);
        }
    }

    public void Unload()
    {
        if (Handle != 0)
            Engine.TextureManager.RemoveTexture(this);
    }

    public override string ToString() => Params.Name ?? Params.Path ?? "Unknown ???";

    private int MaxLevels(int width, int height)
    {
        var maxDim = Math.Max(width, height);
        return (int)Math.Floor(Math.Log(maxDim, 2)) + 1;
    }

    public void Delete()
    {
        if (References > 0)
            Log.Context(this).Warning("Trying to delete a texture with >0 references!");

        GL.DeleteTexture(Handle);
        _memoryTracker?.Dispose();
        _isDeleted = true;
    }
}