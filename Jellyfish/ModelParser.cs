using System.IO;
using Jellyfish.FileFormats.Models;
using Jellyfish.Render;

namespace Jellyfish;

public static class ModelParser
{
    public static MeshPart[]? Parse(string path)
    {
        return Path.GetExtension(path) switch
        {
            ".smd" => SMD.Load(path),
            ".obj" => OBJ.Load(path),
            ".gltf" => GLTF.LoadGLTF(path),
            ".glb" => GLTF.LoadGLB(path),
            ".mdl" => MDL.Load(path[..^4]).Vtx.MeshParts.ToArray(),
            _ => null,
        };
    }
}