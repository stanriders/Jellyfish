using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfish.Console;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public class Model
{
    private readonly List<Mesh> _meshes = new();
    private bool _shouldDraw = true;
    public IReadOnlyList<Mesh> Meshes => _meshes.AsReadOnly();

    public bool ShouldDraw
    {
        get => _shouldDraw;
        set
        {
            foreach (var mesh in _meshes)
            {
                mesh.ShouldDraw = value;
            }
            _shouldDraw = value;
        }
    }


    public Model(string path, bool isDev = false)
    {
        var meshParts = ModelParser.Parse(path);
        if (meshParts == null)
        {
            Log.Context(this).Error("Failed to create Model!");
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
                Log.Context(this).Warning("Mesh {Name} has no texture data!!", meshPart.Name);
            }

            _meshes.Add(new Mesh(meshPart) {IsDev = isDev});
        }

        foreach (var mesh in _meshes)
            MeshManager.AddMesh(mesh);
    }

    public void Unload()
    {
        foreach (var mesh in _meshes)
            MeshManager.RemoveMesh(mesh);
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

    public Quaternion Rotation
    {
        get
        {
            if (_meshes.Any())
                return _meshes[0].Rotation;

            return Quaternion.Identity;
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

    public Vector3 Scale
    {
        get
        {
            if (_meshes.Any())
                return _meshes[0].Scale;

            return Vector3.One;
        }
        set
        {
            if (_meshes.Any())
            {
                foreach (var mesh in _meshes)
                    mesh.Scale = value;
            }
        }
    }
}