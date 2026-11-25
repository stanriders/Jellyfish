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
    public bool EnableCompare { get; set; } = false;
    public required TextureParams TextureParams { get; set; }
}

public class TextureParams
{
    public string? Path { get; set; }
    public string? Name { get; set; }
    public TextureTarget Type { get; set; } = TextureTarget.Texture2D;
    public bool Srgb { get; set; } = false;
    public bool Compress { get; set; }
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
    public int References { get; set; }
    public int Levels { get; private set; }
    public string Format { get; private set; }
    public bool HasAlpha { get; private set; }
    public Vector2 Dimensions { get; private set; }
    public long Size { get; private set; }

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

        var path = Params.Path ?? Params.Name;
        if (!File.Exists(path))
        {
            Log.Context(this).Warning("Texture {Path} doesn't exist!", Params.Name);
            throw new InvalidTextureException($"Texture {Params.Name} doesn't exist!");
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

        if (Params.Compress)
        {
            internalPixelFormat = Params.Srgb ? SizedInternalFormat.CompressedSrgbAlphaBptcUnorm : SizedInternalFormat.CompressedRgbaBptcUnorm;
        }

        if (image.Depth == 16)
        {
            internalPixelFormat = HasAlpha ? SizedInternalFormat.Rgba16 : SizedInternalFormat.Rgb16;
        }

        Levels = CalculateLevels((int)image.Width, (int)image.Height, 8);

        GL.TextureStorage2D(Handle, Levels, internalPixelFormat, (int)image.Width, (int)image.Height);
        GL.TextureSubImage2D(Handle, 0, 0, 0, (int)image.Width, (int)image.Height, pixelFormat, PixelType.UnsignedByte,
            data.GetAreaPointer(0, 0, image.Width, image.Height));

        if (Levels > 1)
            GL.GenerateTextureMipmap(Handle);

        Format = internalPixelFormat.ToString();
        Dimensions = new Vector2(image.Width, image.Height);
        Size = GetGpuSize();

        _memoryTracker = NativeMemoryMeasurement.AddMemory(this, Size);
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

        if (Params.Type == TextureTarget.TextureCubeMap)
        {
            // the global seamless enable is ignored when sampling through bindless handles
            GL.TextureParameteri(Handle, (TextureParameterName)All.TextureCubeMapSeamless, 1);
        }

        Levels = CalculateLevels(RenderTargetParams!.Width, RenderTargetParams.Heigth, 1);

        GL.TextureStorage2D(Handle, Levels, Params.InternalFormat!.Value, RenderTargetParams.Width, RenderTargetParams.Heigth);

        var pixelFormat = Params.InternalFormat.Value switch
        {
            SizedInternalFormat.DepthComponent16 or 
                SizedInternalFormat.DepthComponent24 or
                SizedInternalFormat.DepthComponent32 or 
                SizedInternalFormat.DepthComponent32f => PixelFormat.DepthComponent,
            SizedInternalFormat.Depth24Stencil8 or 
                SizedInternalFormat.Depth32fStencil8 => PixelFormat.DepthStencil,
            _ => PixelFormat.Rgba
        };
        
        var pixelType = Params.InternalFormat.Value switch
        {
            SizedInternalFormat.Depth24Stencil8 => PixelType.UnsignedInt248,
            SizedInternalFormat.Depth32fStencil8 => PixelType.Float32UnsignedInt248Rev,
            _ => PixelType.Float
        };

        // clear the texture to make sure bindless handles don't get garbled on amd
        for (var level = 0; level < Levels; level++)
            GL.ClearTexImage(Handle, level, pixelFormat, pixelType, IntPtr.Zero);

        Format = Params.InternalFormat.ToString()!;
        Dimensions = new Vector2(RenderTargetParams.Width, RenderTargetParams.Heigth);
        Size = GetGpuSize();

        _memoryTracker = NativeMemoryMeasurement.AddMemory(this, Size);
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

    public override string ToString() => Params.Name ?? Params.Path ?? "Unknown ???";

    private int CalculateLevels(int width, int height, int defaultLevels)
    {
        var maxDim = Math.Max(width, height);
        var maxPossibleLevels = (int)Math.Floor(Math.Log(maxDim, 2)) + 1;

        return Params.MaxLevels switch
        {
            -1 => maxPossibleLevels,
            var requested => Math.Clamp(requested ?? defaultLevels, 1, maxPossibleLevels)
        };
    }

    public void Delete()
    {
        if (References > 0)
            Log.Context(this).Warning("Trying to delete a texture with >0 references!");

        GL.DeleteTexture(Handle);
        _memoryTracker?.Dispose();
        _isDeleted = true;
    }

    private long GetGpuSize()
    {
        var faces = Params.Type == TextureTarget.TextureCubeMap ? 6 : 1;

        long total = 0;
        for (var level = 0; level < Levels; level++)
        {
            if (Get(level, All.TextureCompressed) != 0)
            {
                total += (long)Get(level, All.TextureCompressedImageSize) * faces;
                continue;
            }

            var bits = Get(level, All.TextureRedSize) + Get(level, All.TextureGreenSize) +
                       Get(level, All.TextureBlueSize) + Get(level, All.TextureAlphaSize) +
                       Get(level, All.TextureDepthSize) + Get(level, All.TextureStencilSize) +
                       Get(level, All.TextureSharedSize);

            // rgb8, rgb16f, depth24 etc. are padded to the next power of two
            var paddedBits = System.Numerics.BitOperations.RoundUpToPowerOf2((uint)bits);

            total += (long)Get(level, All.TextureWidth) * 
                Get(level, All.TextureHeight) * 
                Get(level, All.TextureDepth) *
                Math.Max(1, Get(level, All.TextureSamples)) * faces * paddedBits / 8;
        }

        return total;

        int Get(int level, All parameter)
        {
            var value = 0;
            GL.GetTextureLevelParameteri(Handle, level, (GetTextureParameter)parameter, ref value);
            return value;
        }
    }
}