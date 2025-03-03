using System.IO;
using Jellyfish.Console;
using Jellyfish.Render.Shaders;
using Newtonsoft.Json;

namespace Jellyfish.Render;

public class Material
{
    public string? Shader { get; set; }
    public string Diffuse { get; set; } = null!;
    public string? Normal { get; set; }
    public string? MetalRoughness { get; set; }

    public Material() { }

    public Material(string path)
    {
        if (!path.EndsWith(".mat"))
        {
            Log.Context(this).Warning("Material {Path} isn't a valid material type, trying to use it as a diffuse texture...", path);
            LoadTextureWithoutMaterial(path);
            return;
        }

        if (!File.Exists(path))
            return;

        var material = JsonConvert.DeserializeObject<Material>(File.ReadAllText(path));
        if (material != null)
        {
            var currentDirectory = Path.GetDirectoryName(path);

            Shader = material.Shader;

            Diffuse = $"{currentDirectory}/{material.Diffuse}";
            if (material.Normal != null) 
                Normal = $"{currentDirectory}/{material.Normal}";

            if (material.MetalRoughness != null)
                MetalRoughness = $"{currentDirectory}/{material.MetalRoughness}";
        }
        else
        {
            Log.Context(this).Error("Material {Path} couldn't be parsed!!", path);
        }
    }

    private void LoadTextureWithoutMaterial(string path)
    {
        Shader = "Main";
        Diffuse = path;
    }

    public Shader GetShaderInstance()
    {
        if (Shader == "Main")
            return new Main(Diffuse, Normal, MetalRoughness);

        // todo: unlit shader
        if (Shader == "Simple")
            return new Main(Diffuse);

        return new Main("materials/error.png");
    }
}