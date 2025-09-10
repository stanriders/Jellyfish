using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Jellyfish.Console;
using Jellyfish.Render.Shaders;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class Material
{
    public string? Name { get; }
    public string? Directory { get; }
    public Shader? Shader { get; }

    private readonly Dictionary<string, object> _params = new();

    public Material(string? path, string? modelName = null)
    {
        var isModel = modelName != null;
        var originalPath = path;

        if (path != null)
        {
            string[] potentialFileNames = [
                $"materials/models/{modelName}/{Path.GetFileNameWithoutExtension(path)}.mat",
                $"materials/models/{modelName}/{Path.GetFileName(path)}",
                $"materials/{Path.GetFileNameWithoutExtension(path)}.mat",
                $"materials/{Path.GetFileName(path)}",
                $"materials/{path}",
                path
            ];

            string? matPath = null;
            foreach (var fileName in potentialFileNames)
            {
                if (File.Exists(fileName))
                {
                    matPath = fileName;
                    break;
                }
            }

            path = matPath;
        }
        else
        {
            if (isModel)
            {
                Log.Context(this).Warning("Mesh {Name} has no texture data!!", modelName);

                var folder = $"materials/models/{modelName}";

                var matPath = $"{folder}/{modelName}.mat";
                if (!File.Exists(matPath))
                {
                    matPath = null;
                }

                path = matPath;
            }
        }

        if (!File.Exists(path))
        {
            Log.Context(this).Warning("Material {Path} doesn't exist!", originalPath);
            path = "materials/error.mat";
        }

        if (!path.EndsWith(".mat"))
        {
            Log.Context(this)
                .Warning("Material {Path} isn't a valid material type, trying to use it as a diffuse texture...", path);

            _params = new Dictionary<string, object>
            {
                { "Shader", "Main" },
                { "Diffuse", path }
            };
        }
        else
        {
            var materialParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path),
                new JsonSerializerSettings { Error = (_, _) => { } });

            if (materialParams == null)
            {
                Log.Context(this).Error("Material {Path} couldn't be parsed!!", path);
                return;
            }

            _params = materialParams;
        }

        Name = Path.GetFileName(path);
        Directory = Path.GetDirectoryName(path);

        var shader = (string?)_params.GetValueOrDefault("Shader");
        if (shader == "Main")
        {
            Shader = new Main(this);
        }
        else if (shader == "Simple")
        {
            // todo: unlit shader
            _params.TryGetValue("Diffuse", out var diffusePath);
            Shader = new Main(Engine.TextureManager.GetTexture(new TextureParams
            {
                Name = $"{Directory}/{diffusePath}",
                Srgb = true
            }).Texture);
        }
        else
        {
            Log.Context(this).Error("No shader defined for material {Path}!", path);
        }
    }

    public T? GetParam<T>(string name)
    {
        var value = _params.GetValueOrDefault(name);
        if (value == null)
            return default;

        if (value is not T valueCasted)
            throw new Exception("Incorrect material param type");

        return valueCasted;
    }

    public bool TryGetParam<T>(string name, out T? value)
    {
        if (_params.TryGetValue(name, out var valueObject))
        {
            if (valueObject is not T valueCasted)
                throw new Exception("Incorrect material param type");

            value = valueCasted;
            return true;
        }

        value = default;
        return false;
    }
    public void Unload()
    {
        Shader?.Unload();
    }
}