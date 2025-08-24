using System;
using System.IO;
using ImageMagick;
using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class Texture
{
    public string Path { get; }
    public int Handle { get; }
    public int References { get; set; } = 1;
    public int Levels { get; set; }
    public string Format { get; set; } = string.Empty;
    public bool Srgb { get; set; }

    private readonly bool _isError;

    public const string error_texture = "materials/error.png";

    public Texture(string path, TextureTarget type, bool srgb)
    {
        Path = path;
        Srgb = srgb;

        if (string.IsNullOrEmpty(path))
            return;

        Handle = GL.CreateTexture(type);

        GL.ObjectLabel(ObjectIdentifier.Texture, (uint)Handle, path.Length, path);

        // procedural textures create themselves
        if (path.StartsWith("_")) 
            return;

        if (!File.Exists(path))
        {
            Log.Context(this).Warning("Texture {Path} doesn't exist!", path);
            path = error_texture;
        }

        if (path == error_texture)
            _isError = true;

        using var image = new MagickImage(path);

        // downsample sRGB-expected textures since ogl doesn't support 16-bit sRGB
        if (image.Depth == 16 && srgb)
        {
            image.Depth = 8;
        }

        using var data = image.GetPixelsUnsafe(); // feels scary

        var hasAlpha = image.ChannelCount == 4;

        var pixelFormat = hasAlpha ? PixelFormat.Rgba : PixelFormat.Rgb;
        var internalPixelFormat = hasAlpha ?
            srgb ? SizedInternalFormat.Srgb8Alpha8 : SizedInternalFormat.Rgba8 :
            srgb ? SizedInternalFormat.Srgb8 : SizedInternalFormat.Rgb8;

        if (image.Depth == 16)
        {
            internalPixelFormat = hasAlpha ? SizedInternalFormat.Rgba16 : SizedInternalFormat.Rgb16;
        }

        var levels = Math.Clamp(Math.Min((int)image.Width, (int)image.Height) / 16, 1, 8);

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TextureStorage2D(Handle, levels, internalPixelFormat, (int)image.Width, (int)image.Height);
        GL.TextureSubImage2D(Handle, 0, 0, 0, (int)image.Width, (int)image.Height, pixelFormat, PixelType.UnsignedByte,
            data.GetAreaPointer(0, 0, image.Width, image.Height));

        GL.GenerateTextureMipmap(Handle);

        Levels = levels;
        Format = internalPixelFormat.ToString();
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

    public override string ToString() => Path;
}