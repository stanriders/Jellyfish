using System.Collections.Generic;
using System.Linq;
using Jellyfish.Render.Shaders;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public static class TextureManager
{
    private static List<Texture> _textures { get; } = new();
    public static IReadOnlyList<Texture> Textures { get; } = _textures.AsReadOnly();

    public static Texture ErrorTexture => GetTexture(Texture.error_texture, TextureTarget.Texture2d, false).Texture;
    public static Material ErrorMaterial => new(new Main(ErrorTexture));

    public static (Texture Texture, bool AlreadyExists) GetTexture(string name, TextureTarget type, bool srgb)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Path == name);
        if (existingTexture != null)
        {
            existingTexture.References++;
            return (existingTexture, true);
        }

        var texture = new Texture(name, type, srgb);
        _textures.Add(texture);

        return (texture, false);
    }

    public static Texture? GetTexture(string name)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Path == name);
        if (existingTexture != null)
        {
            existingTexture.References++;
            return existingTexture;
        }

        return null;
    }

    public static void RemoveTexture(Texture texture)
    {
        texture.References--;

        if (texture.References <= 0)
        {
            _textures.Remove(texture);
            GL.DeleteTexture(texture.Handle);
        }
    }
}