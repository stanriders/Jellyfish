using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;
using YamlDotNet.Serialization;

namespace Jellyfish.Render;

public class Model
{
    private readonly List<Mesh> _meshes = new();

    public Model(string path)
    {
        var meshInfos = ModelParser.Parse(path);

        foreach (var meshInfo in meshInfos)
        {
            var mesh = new Mesh(meshInfo);
            if (meshInfo.Texture != null)
            {
                var modelFolder = $"materials/models/{meshInfo.Name}";
                var matPath = $"{modelFolder}/{Path.GetFileNameWithoutExtension(meshInfo.Texture)}.mat";

                if (File.Exists(matPath))
                {
                    var deserializer = new Deserializer();
                    var material = deserializer.Deserialize<Material>(File.ReadAllText(matPath));

                    mesh.AddShader(
                        new Main($"{modelFolder}/{material.Diffuse}", material.Normal != null ? $"{modelFolder}/{material.Normal}" : null));
                }
                else
                {
                    mesh.AddShader(new Main("materials/error.png"));
                }
            }
            else
            {
                mesh.AddShader(new Main("materials/error.png"));
            }

            _meshes.Add(mesh);
        }

        foreach (var mesh in _meshes)
            MeshManager.AddMesh(mesh);
    }

    public Vector3 Position
    {
        get
        {
            if (_meshes.Any())
                return _meshes[0].Position;

            return Vector3.Zero;
        }
        set
        {
            foreach (var mesh in _meshes)
                mesh.Position = value;
        }
    }

    public Vector3 Rotation
    {
        get
        {
            if (_meshes.Any())
                return _meshes[0].Rotation;

            return Vector3.Zero;
        }
        set
        {
            foreach (var mesh in _meshes)
                mesh.Rotation = value;
        }
    }
}