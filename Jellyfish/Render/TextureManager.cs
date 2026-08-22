using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class TextureManager
{
    private readonly List<Texture> _textures = new();
    public IReadOnlyList<Texture> Textures => _textures.AsReadOnly();

    private const string error_texture = "_engine_Error";

    public TextureManager()
    {
        CreateTexture(new TextureParams
        {
            Name = error_texture, 
            Path = "materials/error.png", 
            MagFiltering = TextureMagFilter.Nearest, 
            MinFiltering = TextureMinFilter.Nearest
        });
    }

    public Texture CreateTexture(TextureParams textureParams)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Path == (textureParams.Path ?? textureParams.Name) || 
                                                            x.Params.Name == (textureParams.Name ?? textureParams.Path));
        if (existingTexture != null)
            throw new Exception($"Texture {textureParams.Name} already exists");

        try
        {
            var texture = new Texture(textureParams);
            _textures.Add(texture);

            return texture;
        }
        catch (InvalidTextureException)
        {
            return GetTexture(error_texture)!;
        }
    }

    public Texture CreateTexture(RenderTargetParams rtParams)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Name == rtParams.TextureParams.Name);
        if (existingTexture != null)
            throw new Exception($"Texture {rtParams.TextureParams.Name} already exists");

        try
        {
            var texture = new Texture(rtParams);
            _textures.Add(texture);

            return texture;
        }
        catch (InvalidTextureException)
        {
            return GetTexture(error_texture)!;
        }
    }

    public (Texture Texture, bool AlreadyExists) GetTexture(TextureParams textureParams)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Path == (textureParams.Path ?? textureParams.Name) ||
                                                            x.Params.Name == (textureParams.Name ?? textureParams.Path));
        if (existingTexture != null)
        {
            existingTexture.References++;
            return (existingTexture, true);
        }

        try
        {
            var texture = new Texture(textureParams);
            _textures.Add(texture);

            return (texture, false);
        }
        catch (InvalidTextureException)
        {
            return (GetTexture(error_texture)!, false);
        }
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
            texture.Delete();
        }
    }
}