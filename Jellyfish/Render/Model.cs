using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfish.Console;
using Jellyfish.Utils;
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
        Log.Context(this).Information("Loading model {Path}...", path);

        var meshParts = ModelParser.Parse(path);
        if (meshParts.Length <= 0)
        {
            Log.Context(this).Error("Failed to create Model!");
            return;
        }

        foreach (var meshPart in meshParts)
        {
            meshPart.IsDev = isDev;
            _meshes.Add(meshPart);
        }

        foreach (var mesh in _meshes)
            Engine.MeshManager.AddMesh(mesh);
    }

    public Model(Mesh mesh, bool isDev = false)
    {
        mesh.IsDev = isDev;
        _meshes.Add(mesh);
        Engine.MeshManager.AddMesh(mesh);
    }

    public void Unload()
    {
        foreach (var mesh in _meshes)
            Engine.MeshManager.RemoveMesh(mesh);
    }

    public Vector3 Position
    {
        get
        {
            if (_meshes.Count != 0)
                return _meshes[0].Position;

            return Vector3.Zero;
        }
        set
        {
            if (_meshes.Count != 0)
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
            if (_meshes.Count != 0)
                return _meshes[0].Rotation;

            return Quaternion.Identity;
        }
        set
        {
            if (_meshes.Count != 0)
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
            if (_meshes.Count != 0)
                return _meshes[0].Scale;

            return Vector3.One;
        }
        set
        {
            if (_meshes.Count != 0)
            {
                foreach (var mesh in _meshes)
                    mesh.Scale = value;
            }
        }
    }

    public BoundingBox BoundingBox => new BoundingBox(_meshes.Select(x => x.BoundingBox).ToArray());
}