using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class TextureManager
{
    private readonly List<Texture> _textures = new();
    public IReadOnlyList<Texture> Textures => _textures.AsReadOnly();

    public (Texture Texture, bool AlreadyExists) GetTexture(string name, TextureTarget type, bool srgb)
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

    public Texture? GetTexture(string name)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Path == name);
        if (existingTexture != null)
        {
            existingTexture.References++;
            return existingTexture;
        }

        return null;
    }

    public void RemoveTexture(Texture texture)
    {
        texture.References--;

        if (texture.References <= 0)
        {
            _textures.Remove(texture);
            GL.DeleteTexture(texture.Handle);
        }
    }
}