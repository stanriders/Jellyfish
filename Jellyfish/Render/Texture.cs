using System.IO;
using ImageMagick;
using OpenTK.Graphics.OpenGL;
using Serilog;

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

        var textureHandles = new int[1];
        GL.CreateTextures(type, 1, textureHandles);
        Handle = textureHandles[0];

        GL.ObjectLabel(ObjectLabelIdentifier.Texture, Handle, path.Length, path);

        // procedural textures create themselves
        if (path.StartsWith("_")) 
            return;

        if (!File.Exists(path))
        {
            Log.Warning("[Texture] Texture {Path} doesn't exist!", path);
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

        //GL.TextureParameter(_handle, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        //GL.TextureParameter(_handle, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TextureParameter(Handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(Handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TextureStorage2D(Handle, 1, internalPixelFormat, image.Width, image.Height);
        GL.TextureSubImage2D(Handle, 0, 0, 0, image.Width, image.Height, pixelFormat, PixelType.UnsignedByte,
            data.GetAreaPointer(0, 0, image.Width, image.Height));

        GL.GenerateTextureMipmap(Handle);
    }

    public void Bind(int unit)
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