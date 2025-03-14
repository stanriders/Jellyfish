using Jellyfish.Render;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using OpenTK.Mathematics;
using JoltPhysicsSharp;

namespace Jellyfish.Entities;

[Entity("plane_bezier")]
public class BezierPlaneEntity : BaseEntity, IPhysicsEntity
{
    private Mesh? _plane;
    private BodyID _physicsBodyId;

    public BezierPlaneEntity()
    {
        AddProperty("Size", new Vector2(20, 20), false);
        AddProperty("QuadSize", 2, false);
        AddProperty("Texture", "test.png", false);
        AddProperty("TextureScale", new Vector2(1.0f), false);
    }

    public override void Load()
    {
        var texture = GetPropertyValue<string>("Texture");
        if (texture == null)
        {
            EntityLog().Error("Texture not set!");
            return;
        }

        _plane = new Mesh(GenerateRandom(GetPropertyValue<Vector2>("Size"), texture, GetPropertyValue<int>("QuadSize")))
        {
            Position = GetPropertyValue<Vector3>("Position"),
            Rotation = GetPropertyValue<Quaternion>("Rotation")
        };

        MeshManager.AddMesh(_plane);
        _physicsBodyId = PhysicsManager.AddStaticObject(new[] { _plane.MeshPart }, this) ?? 0;
        base.Load();
    }

    protected override void OnPositionChanged(Vector3 position)
    {
        if (_plane != null)
        {
            _plane.Position = position;
        }

        base.OnPositionChanged(position);
    }

    protected override void OnRotationChanged(Quaternion rotation)
    {
        if (_plane != null)
        {
            _plane.Rotation = rotation;
        }

        base.OnRotationChanged(rotation);
    }

    public override void Unload()
    {
        if (_plane != null)
            MeshManager.RemoveMesh(_plane);

        PhysicsManager.RemoveObject(_physicsBodyId);

        base.Unload();
    }

    /// <summary>
    /// https://github.com/tugbadogan/opengl-bezier-surface/tree/master
    /// </summary>
    private MeshPart GenerateRandom(Vector2 size, string texture, int quadSize)
    {
        List<Vertex> verticies = new();

        var watch = Stopwatch.StartNew();

        var sizeX = (int)size.X;
        var sizeY = (int)size.Y;

        var resolutionX = sizeX;
        var resolutionY = sizeY;

        var inPoints = new double[sizeX + 1, sizeY + 1][];
        var outPoints = new double[resolutionX, resolutionY][];

        for (var x = 0; x <= sizeX; x++)
        {
            for (var y = 0; y <= sizeY; y++)
            {
                inPoints[x, y] = [x * quadSize - 0.5, y * quadSize - 0.5, (Random.Shared.NextDouble() * quadSize) - (quadSize / 2.0)];
            }
        }

        for (var i = 0; i < resolutionX; i++)
        {
            var muX = i / (float)(resolutionX - 1);
            for (var j = 0; j < resolutionY; j++)
            {
                var muY = j / (float)(resolutionY - 1);

                outPoints[i, j] = [0, 0, 0];

                for (var kX = 0; kX <= sizeX; kX++)
                {
                    var bi = BezierBlend(kX, muX, sizeX);
                    for (var kY = 0; kY <= sizeY; kY++)
                    {
                        var bj = BezierBlend(kY, muY, sizeY);
                        outPoints[i, j][0] += inPoints[kX, kY][0] * bi * bj;
                        outPoints[i, j][1] += inPoints[kX, kY][1] * bi * bj;
                        outPoints[i, j][2] += inPoints[kX, kY][2] * bi * bj;
                    }
                }
            }
        }

        /* Display the surface, in this case in OOGL format for GeomView */
        for (var i = 0; i < resolutionX - 1; i++)
        {
            for (var j = 0; j < resolutionY - 1; j++)
            {
                var a = new Vector3((float)outPoints[i, j][0], (float)outPoints[i, j][1], (float)outPoints[i, j][2]);
                var d = new Vector3((float)outPoints[i, j + 1][0], (float)outPoints[i, j + 1][1],
                    (float)outPoints[i, j + 1][2]);
                var b = new Vector3((float)outPoints[i + 1, j][0], (float)outPoints[i + 1, j][1],
                    (float)outPoints[i + 1, j][2]);
                var c = new Vector3((float)outPoints[i + 1, j + 1][0], (float)outPoints[i + 1, j + 1][1],
                    (float)outPoints[i + 1, j + 1][2]);

                Vector3 u = b - a;
                Vector3 v = c - b;

                Vector3 normal = Vector3.Cross(u, v).Normalized();

                var textureScale = GetPropertyValue<Vector2>("TextureScale");
                var uvScale = new Vector2(size.X / quadSize * textureScale.X, size.Y / quadSize * textureScale.Y);

                verticies.AddRange(new Vertex[]
                {
                    new()
                    {
                        Coordinates = a,
                        Normal = normal,
                        UV = new(uvScale.X, uvScale.Y)
                    },
                    new()
                    {
                        Coordinates = b,
                        Normal = normal,
                        UV = new(-uvScale.X, uvScale.Y)
                    },
                    new()
                    {
                        Coordinates = c,
                        Normal = normal,
                        UV = new(-uvScale.X, -uvScale.Y)
                    },
                    new()
                    {
                        Coordinates = a,
                        Normal = normal,
                        UV = new(uvScale.X, uvScale.Y)
                    },
                    new()
                    {
                        Coordinates = c,
                        Normal = normal,
                        UV = new(-uvScale.X, -uvScale.Y)
                    },
                    new()
                    {
                        Coordinates = d,
                        Normal = normal,
                        UV = new(uvScale.X, -uvScale.Y)
                    },
                });
            }
        }

        watch.Stop();
        EntityLog().Information("Took {Elapsed} time to create a plane", watch.Elapsed);

        return new MeshPart
        {
            Name = "randombezierplane",
            Vertices = verticies,
            Texture = $"materials/{texture}"
        };
    }

    private float BezierBlend(int k, float mu, int n)
    {
        float blend = 1;

        var nn = n;
        var kn = k;
        var nkn = n - k;

        while (nn >= 1)
        {
            blend *= nn;
            nn--;
            if (kn > 1)
            {
                blend /= kn;
                kn--;
            }

            if (nkn > 1)
            {
                blend /= nkn;
                nkn--;
            }
        }

        if (k > 0)
            blend *= (float)Math.Pow(mu, k);
        if (n - k > 0)
            blend *= (float)Math.Pow(1 - mu, n - k);

        return blend;
    }

    public void OnPhysicsPositionChanged(Vector3 position)
    {
    }

    public void OnPhysicsRotationChanged(Quaternion rotation)
    {
    }
}