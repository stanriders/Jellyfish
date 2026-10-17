using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public sealed class TextureRef : IDisposable
{
    public Texture Texture { get; }

    public TextureRef(Texture texture)
    {
        Texture = texture;
        Texture.References++;
    }

    public void Dispose()
    {
        Texture.References--;
        if (Texture.References <= 0)
            Engine.TextureManager.RemoveTexture(Texture);
    }
}

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

    public TextureRef CreateTexture(TextureParams textureParams)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Path == (textureParams.Path ?? textureParams.Name) || 
                                                            x.Params.Name == (textureParams.Name ?? textureParams.Path));
        if (existingTexture != null)
            throw new Exception($"Texture {textureParams.Name} already exists");

        try
        {
            var texture = new Texture(textureParams);
            _textures.Add(texture);

            return new TextureRef(texture);
        }
        catch (InvalidTextureException)
        {
            return GetTexture(error_texture)!;
        }
    }

    public TextureRef CreateTexture(RenderTargetParams rtParams)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Name == rtParams.TextureParams.Name);
        if (existingTexture != null)
            throw new Exception($"Texture {rtParams.TextureParams.Name} already exists");

        try
        {
            var texture = new Texture(rtParams);
            _textures.Add(texture);

            return new TextureRef(texture);
        }
        catch (InvalidTextureException)
        {
            return GetTexture(error_texture)!;
        }
    }

    public (TextureRef Texture, bool AlreadyExists) GetTexture(TextureParams textureParams)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Path == (textureParams.Path ?? textureParams.Name) ||
                                                            x.Params.Name == (textureParams.Name ?? textureParams.Path));
        if (existingTexture != null)
        {
            return (new TextureRef(existingTexture), true);
        }

        return (CreateTexture(textureParams), false);
    }

    public TextureRef? GetTexture(string name)
    {
        var existingTexture = _textures.FirstOrDefault(x => x.Params.Name == name);
        if (existingTexture != null)
        {
            return new TextureRef(existingTexture);
        }

        return null;
    }

    public void RemoveTexture(Texture texture)
    {
        _textures.Remove(texture);
        texture.Delete();
    }
}