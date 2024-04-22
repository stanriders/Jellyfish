using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfish.Render.Shaders;
using Newtonsoft.Json;
using OpenTK.Mathematics;
using Serilog;
using SharpGLTF.Schema2;

namespace Jellyfish.Render;

public class Model
{
    private readonly List<Mesh> _meshes = new();

    public Model(string path)
    {
        var meshInfos = ModelParser.Parse(path);
        if (meshInfos == null)
        {
            Log.Error("[Model] Failed to create Model!");
            return;
        }

        foreach (var meshInfo in meshInfos)
        {
            if (meshInfo.Texture != null)
            {
                var modelFolder = $"materials/models/{meshInfo.Name}";
                var matPath = $"{modelFolder}/{Path.GetFileNameWithoutExtension(meshInfo.Texture)}.mat";
                meshInfo.Texture = matPath;
            }
            else
            {
                Log.Warning("[Model] Mesh {Name} has no texture data!!", meshInfo.Name);
            }

            _meshes.Add(new Mesh(meshInfo));
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