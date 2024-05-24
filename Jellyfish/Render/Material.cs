using System.IO;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders;
using Jellyfish.Render.Shaders.Deferred;
using Newtonsoft.Json;
using Serilog;

namespace Jellyfish.Render;

public class Material
{
    public string? Shader { get; set; }
    public string Diffuse { get; set; } = null!;
    public string? Normal { get; set; }
    public bool Phong { get; set; }
    public int? PhongExponent { get; set; }

    public Material() { }

    public Material(string path)
    {
        if (!path.EndsWith(".mat"))
        {
            Log.Information("Material {Path} isn't a valid material type, trying to use it as a diffuse texture...", path);
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

            Phong = material.Phong;
            PhongExponent = material.PhongExponent;
        }
        else
        {
            Log.Warning("[Material] Material {Path} couldn't be parsed!!", path);
        }
    }

    private void LoadTextureWithoutMaterial(string path)
    {
        Shader = "Main";
        Diffuse = path;
        Phong = false;
    }

    public Shader GetShaderInstance(VertexArray vao)
    {
        // TODO: redo vertex attrib handling, this is getting absurd
        if (Shader == "Main")
            return new Main(vao, Diffuse, Normal, Phong, PhongExponent ?? 16);

        return new Main(vao, "materials/error.png");
    }
}