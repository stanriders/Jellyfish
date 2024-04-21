using System.IO;
using Jellyfish.Render.Shaders;
using Newtonsoft.Json;
using Serilog;

namespace Jellyfish.Render;

public class Material
{
    public string? Shader { get; set; }
    public string Diffuse { get; set; } = null!;
    public string? Normal { get; set; }
    public string? Phong { get; set; }

    public Material(string path)
    {
        if (!File.Exists(path)) 
            return;

        var material = JsonConvert.DeserializeObject<Material>(File.ReadAllText(path));
        if (material != null)
        {
            var currentDirectory = Path.GetDirectoryName(path);

            Shader = $"{currentDirectory}/{material.Shader}";
            Diffuse = $"{currentDirectory}/{material.Diffuse}";
            if (material.Normal != null) 
                Normal = $"{currentDirectory}/{material.Normal}";
            if (material.Phong != null)
                Phong = $"{currentDirectory}/{material.Phong}";
        }
        else
        {
            Log.Warning("[Material] Material {Path} couldn't be parsed!!", path);
        }

    }

    public Shader GetShaderInstance()
    {
        if (Shader == null)
            return new Main("materials/error.png");

        return new Main(Diffuse, Normal);
    }
}