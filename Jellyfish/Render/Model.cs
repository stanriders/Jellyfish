using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Render;

public class Model
{
    private readonly List<Mesh> _meshes = new();
    public IReadOnlyList<Mesh> Meshes => _meshes.AsReadOnly();

    public Model(string path)
    {
        var meshParts = ModelParser.Parse(path);
        if (meshParts == null)
        {
            Log.Error("[Model] Failed to create Model!");
            return;
        }

        foreach (var meshPart in meshParts)
        {
            if (meshPart.Texture != null)
            {
                var modelFolder = $"materials/models/{meshPart.Name}";
                var matPath = $"{modelFolder}/{Path.GetFileNameWithoutExtension(meshPart.Texture)}.mat";
                if (!File.Exists(matPath))
                    matPath = $"{modelFolder}/{Path.GetFileName(meshPart.Texture)}";

                meshPart.Texture = matPath;
            }
            else
            {
                Log.Warning("[Model] Mesh {Name} has no texture data!!", meshPart.Name);
            }

            _meshes.Add(new Mesh(meshPart));
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
            if (_meshes.Any())
            {
                foreach (var mesh in _meshes)
                    mesh.Position = value;
            }
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
            if (_meshes.Any())
            {
                foreach (var mesh in _meshes)
                    mesh.Rotation = value;
            }
        }
    }
}