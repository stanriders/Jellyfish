using System.Collections.Generic;
using System.IO;
using Jellyfish.Console;
using Jellyfish.Render.Shaders;
using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class Material
{
    public Dictionary<string, string> Params { get; } = new();
    public string? Directory { get; }
    public Shader? Shader { get; }

    public Material(Shader shader)
    {
        Shader = shader;
    }

    public Material(string? path, string? modelName = null)
    {
        Shader = TextureManager.ErrorMaterial.Shader;

        var isModel = modelName != null;
        var originalPath = path;

        if (path != null)
        {
            var folder = isModel ? $"materials/models/{modelName}" : "materials";

            var matPath = $"{folder}/{Path.GetFileNameWithoutExtension(path)}.mat";
            if (!File.Exists(matPath))
            {
                matPath = $"{folder}/{Path.GetFileName(path)}";
                if (!File.Exists(matPath))
                {
                    matPath = path;
                    if (!File.Exists(matPath))
                    {
                        matPath = null;
                    }
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
            return;
        }

        if (!path.EndsWith(".mat"))
        {
            Log.Context(this)
                .Warning("Material {Path} isn't a valid material type, trying to use it as a diffuse texture...", path);

            Params = new Dictionary<string, string>
            {
                { "Shader", "Main" },
                { "Diffuse", path }
            };
        }

        var materialParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path),
            new JsonSerializerSettings { Error = (_, _) => { } });

        if (materialParams == null)
        {
            Log.Context(this).Error("Material {Path} couldn't be parsed!!", path);
            return;
        }

        Params = materialParams;
        Directory = Path.GetDirectoryName(path);

        var shader = Params.GetValueOrDefault("Shader");
        if (shader == "Main")
        {
            Shader = new Main(this);
        }
        else if (shader == "Simple")
        {
            // todo: unlit shader
            Params.TryGetValue("Diffuse", out var diffusePath);
            Shader = new Main(TextureManager.GetTexture($"{Directory}/{diffusePath}", TextureTarget.Texture2d, true).Texture);
        }
        else
        {
            Log.Context(this).Error("No shader defined for material {Path}!", path);
        }
    }

    public void Unload()
    {
        Shader?.Unload();
    }
}