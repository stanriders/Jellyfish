using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class TextureManager
{
    private readonly List<Texture> _textures = new();
    public IReadOnlyList<Texture> Textures => _textures.AsReadOnly();

    public Texture CreateTexture(TextureParams textureParams)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Name == textureParams.Name);
        if (existingTexture != null)
            throw new Exception($"Texture {textureParams.Name} already exists");

        var texture = new Texture(textureParams);
        _textures.Add(texture);

        return texture;
    }

    public (Texture Texture, bool AlreadyExists) GetTexture(TextureParams textureParams)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Name == textureParams.Name);
        if (existingTexture != null)
        {
            existingTexture.References++;
            return (existingTexture, true);
        }

        var texture = new Texture(textureParams);
        _textures.Add(texture);

        return (texture, false);
    }

    public Texture? GetTexture(string name)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Name == name);
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