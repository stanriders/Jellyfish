using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Render;

public class BezierPlane : Mesh
{
    public Vector2 Size { get; set; }
    public int QuadSize { get; set; }

    public BezierPlane(Vector2 size, string texture, int quadSize)
    {
        Size = size;
        QuadSize = quadSize;

        MeshPart = GenerateRandom();
        CreateBuffers();
        AddMaterial($"materials/{texture}");
    }

    /// <summary>
    /// https://github.com/tugbadogan/opengl-bezier-surface/tree/master
    /// </summary>
    private MeshPart GenerateRandom()
    {
        List<Vertex> verticies = new();

        var watch = Stopwatch.StartNew();

        var sizeX = (int)Size.X;
        var sizeY = (int)Size.Y;

        var resolutionX = sizeX;
        var resolutionY = sizeY;

        var inPoints = new double[sizeX + 1, sizeY + 1][];
        var outPoints = new double[resolutionX, resolutionY][];
        
        for (var x = 0; x <= sizeX; x++)
        {
            for (var y = 0; y <= sizeY; y++)
            {
                inPoints[x, y] = [x * QuadSize - 0.5, y * QuadSize - 0.5, (Random.Shared.Next() % 10000) / 5000.0 - 1];
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

                verticies.AddRange(new Vertex[]
                {
                    new()
                    {
                        Coordinates = a,
                        Normal = normal,
                        UV = new(1f, 1f)
                    },
                    new()
                    {
                        Coordinates = b,
                        Normal = normal,
                        UV = new(-1f, 1f)
                    },
                    new()
                    {
                        Coordinates = c,
                        Normal = normal,
                        UV = new(-1f, -1f)
                    },
                    new()
                    {
                        Coordinates = a,
                        Normal = normal,
                        UV = new(1f, 1f)
                    },
                    new()
                    {
                        Coordinates = c,
                        Normal = normal,
                        UV = new(-1f, -1f)
                    },
                    new()
                    {
                        Coordinates = d,
                        Normal = normal,
                        UV = new(1f, -1f)
                    },
                });
            }
        }

        watch.Stop();
        Log.Information("[BezierPlane] Took {Elapsed} time to create a plane", watch.Elapsed);

        return new MeshPart
        {
            Name = "randombezierplane",
            Vertices = verticies
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
}