﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfish.Render.Shaders;
using OpenTK;

namespace Jellyfish.Render
{
    class Model
    {
        private readonly List<Mesh> meshes = new List<Mesh>();

        public Vector3 Position
        {
            get
            {
                if (meshes.Any())
                    return meshes[0].Position;

                return Vector3.Zero;
            }
            set
            {
                foreach (var mesh in meshes)
                {
                    mesh.Position = value;
                }
            }
        }

        public Vector3 Rotation
        {
            get
            {
                if (meshes.Any())
                    return meshes[0].Rotation;

                return Vector3.Zero;
            }
            set
            {
                foreach (var mesh in meshes)
                {
                    mesh.Rotation = value;
                }
            }
        }

        public Model(string path)
        {
            var meshInfos = ModelParser.Parse(path);

            foreach (var meshInfo in meshInfos)
            {
                var mesh = new Mesh(meshInfo);
                if (meshInfo.Texture != null)
                {
                    var matPath = $"materials/models/{Path.GetFileNameWithoutExtension(meshInfo.Texture)}.mat";

                    if (File.Exists(matPath))
                    {
                        var deserializer = new YamlDotNet.Serialization.Deserializer();
                        var material = deserializer.Deserialize<Material>(File.ReadAllText(matPath));

                        mesh.AddShader(
                            new Main($"materials/models/{material.Diffuse}", $"materials/models/{material.Normal}"));
                    }
                    else
                        mesh.AddShader(new Main("materials/error.png"));
                }
                else
                    mesh.AddShader(new Main("materials/error.png"));

                meshes.Add(mesh);
            }

            foreach (var mesh in meshes)
            {
                MeshManager.AddMesh(mesh);
            }
        }
    }
}
