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
    
    private readonly bool _isError;

    public const string error_texture = "materials/error.png";

    public Texture(string path, TextureTarget type)
    {
        Path = path;

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
        using var data = image.GetPixelsUnsafe(); // feels scary

        var pixelFormat = PixelFormat.Rgba;
        var internalPixelFormat = SizedInternalFormat.Rgba8;
        if (image is { ChannelCount: 3, Depth: 8 })
        {
            pixelFormat = PixelFormat.Rgb;
            internalPixelFormat = SizedInternalFormat.Rgb8;
        }

        GL.TextureParameteri(Handle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameteri(Handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TextureStorage2D(Handle, 4, internalPixelFormat, (int)image.Width, (int)image.Height);
        GL.TextureSubImage2D(Handle, 0, 0, 0, (int)image.Width, (int)image.Height, pixelFormat, PixelType.UnsignedByte,
            data.GetAreaPointer(0, 0, image.Width, image.Height));

        GL.GenerateTextureMipmap(Handle);
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
            TextureManager.RemoveTexture(this);
    }
}