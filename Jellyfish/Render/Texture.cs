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

        _handle = GL.GenTexture();
        Bind();

        if (!File.Exists(path))
        {
            Log.Warning("[Texture] Texture {Path} doesn't exist!", path);
            path = error_texture;
        }

        using var image = new MagickImage(path);
        using var data = image.GetPixelsUnsafe(); // feels scary

        GL.TexImage2D(TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba,
            image.Width,
            image.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            data.GetAreaPointer(0, 0, image.Width, image.Height));

        //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind(TextureUnit unit = TextureUnit.Texture0)
    {
        if (_handle != 0)
        {
            GL.ActiveTexture(unit);
            GL.BindTexture(TextureTarget.Texture2D, _handle);
        }
    }

    public int GetHandle()
    {
        return _handle;
    }
}