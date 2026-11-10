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
    private readonly Dictionary<int, Texture> _textures = new();
    public IReadOnlyList<Texture> Textures => _textures.Values.ToList();

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

    private int BuildHash(string? name, string? path)
    {
        var hash = new HashCode();
        hash.Add(name);
        hash.Add(path);
        return hash.ToHashCode();
    }

    public TextureRef CreateTexture(TextureParams textureParams)
    {
        var nameHash = BuildHash(textureParams.Name, textureParams.Path);

        if (_textures.ContainsKey(nameHash))
            throw new Exception($"Texture {textureParams.Name} already exists");

        try
        {
            var texture = new Texture(textureParams);
            _textures.Add(nameHash, texture);

            return new TextureRef(texture);
        }
        catch (InvalidTextureException)
        {
            return GetTexture(error_texture)!;
        }
    }

    public TextureRef CreateTexture(RenderTargetParams rtParams)
    {
        var nameHash = BuildHash(rtParams.TextureParams.Name, null);

        if (_textures.ContainsKey(nameHash))
            throw new Exception($"Texture {rtParams.TextureParams.Name} already exists");

        try
        {
            var texture = new Texture(rtParams);
            _textures.Add(nameHash, texture);

            return new TextureRef(texture);
        }
        catch (InvalidTextureException)
        {
            return GetTexture(error_texture)!;
        }
    }

    public (TextureRef Texture, bool AlreadyExists) GetTexture(TextureParams textureParams)
    {
        var nameHash = BuildHash(textureParams.Name, textureParams.Path);

        if (_textures.TryGetValue(nameHash, out var texture))
        {
            return (new TextureRef(texture), true);
        }

        return (CreateTexture(textureParams), false);
    }

    public TextureRef? GetTexture(string name)
    {
        var nameHash = BuildHash(name, null);

        if (_textures.TryGetValue(nameHash, out var texture))
        {
            return new TextureRef(texture);
        }

        return null;
    }

    public void RemoveTexture(Texture texture)
    {
        _textures.Remove(_textures.FirstOrDefault(x => x.Value == texture).Key);
        texture.Delete();
    }
}