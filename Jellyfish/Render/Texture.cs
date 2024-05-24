using System.IO;
using ImageMagick;
using OpenTK.Graphics.OpenGL;
using Serilog;

namespace Jellyfish.Render;

public class Texture
{
    private readonly int _handle;

    public const string error_texture = "materials/error.png";

    public Texture(string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        // we get a handle before checking for path existing to be able to see which textures failed to load in the texture list
        (_handle, var alreadyExists) = TextureManager.GenerateHandle(path, TextureTarget.Texture2D);
        if (alreadyExists)
            return;

        GL.ObjectLabel(ObjectLabelIdentifier.Texture, _handle, path.Length, path);

        if (!File.Exists(path))
        {
            Log.Warning("[Texture] Texture {Path} doesn't exist!", path);
            path = error_texture;
        }

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
        GL.TextureParameter(_handle, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TextureParameter(_handle, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TextureStorage2D(_handle, 1, internalPixelFormat, image.Width, image.Height);
        GL.TextureSubImage2D(_handle, 0, 0, 0, image.Width, image.Height, pixelFormat, PixelType.UnsignedByte, data.GetAreaPointer(0, 0, image.Width, image.Height));

        GL.GenerateTextureMipmap(_handle);
    }

    public void Bind(int unit)
    {
        if (_handle != 0)
        {
            GL.BindTextureUnit(unit, _handle);
        }
    }
}