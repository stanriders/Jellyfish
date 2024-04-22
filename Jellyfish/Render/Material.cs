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
    public bool Phong { get; set; }
    public int? PhongExponent { get; set; }

    public Material(string path)
    {
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

    public Shader GetShaderInstance()
    {
        if (Shader == "Main")
            return new Main(Diffuse, Normal, Phong, PhongExponent ?? 16);

        return new Main("materials/error.png");
    }
}