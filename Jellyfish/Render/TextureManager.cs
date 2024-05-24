using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public static class TextureManager
{
    private static Dictionary<string, int> _textures { get; } = new();
    public static IReadOnlyDictionary<string, int> Textures { get; } = _textures.AsReadOnly();

    public static (int handle, bool alreadyExists) GenerateHandle(string name, TextureTarget target)
    {
        if (_textures.TryGetValue(name, out var handle))
        {
            return (handle, true);
        }

        var textureHandles = new int[1];
        GL.CreateTextures(target, 1, textureHandles);
        handle = textureHandles[0];
        _textures.Add(name, handle);

        return (handle, false);
    }

    public static int GenerateHandle(string name)
    {
        if (_textures.TryGetValue(name, out var handle))
        {
            return handle;
        }

        handle = GL.GenTexture();
        _textures.Add(name, handle);

        return handle;
    }
}