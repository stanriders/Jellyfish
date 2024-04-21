using System;
using System.Collections.Generic;
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public class BezierPlane : Mesh
{
    public Vector2 Size { get; set; }
    public int Resolution { get; set; }

    public BezierPlane(Vector2 size, int resolution, string texture)
    {
        Size = size;
        Resolution = resolution;

        MeshInfo = GenerateRandom();
        CreateBuffers();
        AddMaterial($"materials/{texture}");
    }

    /// <summary>
    /// https://github.com/tugbadogan/opengl-bezier-surface/tree/master
    /// </summary>
    private MeshInfo GenerateRandom()
    {
        List<Vector3> points = new();
        List<Vector3> normals = new();

        var sizeX = (int)Size.X;
        var sizeY = (int)Size.Y;

        var resolutionX = Resolution * sizeX;
        var resolutionY = Resolution * sizeY;

        var inPoints = new double[sizeX + 1, sizeY + 1, 3];
        var outPoints = new double[resolutionX, resolutionY, 3];

        for (var x = 0; x <= sizeX; x++)
        {
            for (var y = 0; y <= sizeY; y++)
            {
                inPoints[x, y, 0] = x / 5.0 - 0.5;
                inPoints[x, y, 1] = y / 5.0 - 0.5;
                inPoints[x, y, 2] = Random.Shared.Next(5);
            }
        }

        for (var i = 0; i < resolutionX; i++)
        {
            var mui = i / (float)(resolutionX - 1);
            for (var j = 0; j < resolutionY; j++)
            {
                var muj = j / (float)(resolutionY - 1);
                outPoints[i, j, 0] = 0;
                outPoints[i, j, 1] = 0;
                outPoints[i, j, 2] = 0;
                for (var ki = 0; ki <= sizeX; ki++)
                {
                    var bi = BezierBlend(ki, mui, sizeX);
                    for (var kj = 0; kj <= sizeY; kj++)
                    {
                        var bj = BezierBlend(kj, muj, sizeY);
                        outPoints[i, j, 0] += inPoints[ki, kj, 0] * bi * bj;
                        outPoints[i, j, 1] += inPoints[ki, kj, 1] * bi * bj;
                        outPoints[i, j, 2] += inPoints[ki, kj, 2] * bi * bj;
                    }
                }
            }
        }

        /* Display the surface, in this case in OOGL format for GeomView */
        for (var i = 0; i < resolutionX - 1; i++)
        {
            for (var j = 0; j < resolutionY - 1; j++)
            {
                var a = new Vector3((float)outPoints[i, j, 0], (float)outPoints[i, j, 1], (float)outPoints[i, j, 2]);
                var b = new Vector3((float)outPoints[i, j + 1, 0], (float)outPoints[i, j + 1, 1], (float)outPoints[i, j + 1, 2]);
                var c = new Vector3((float)outPoints[i + 1, j, 0], (float)outPoints[i + 1, j, 1], (float)outPoints[i + 1, j, 2]);
                var d = new Vector3((float)outPoints[i + 1, j + 1, 0], (float)outPoints[i + 1, j + 1, 1],
                    (float)outPoints[i + 1, j + 1, 2]);

                Vector3 u = b - a;
                Vector3 v = c - b;

                Vector3 normal = Vector3.Cross(u, v).Normalized();

                normals.Add(normal);
                points.Add(a);

                normals.Add(normal);
                points.Add(b);

                normals.Add(normal);
                points.Add(c);

                normals.Add(normal);
                points.Add(a);

                normals.Add(normal);
                points.Add(c);

                normals.Add(normal);
                points.Add(d);
            }
        }

        return new MeshInfo
        {
            Name = "randombezierplane",
            Vertices = points,
            Normals = normals
        };
    }

    private float BezierBlend(int k, float mu, int n)
    {
        int nn, kn, nkn;
        float blend = 1;

        nn = n;
        kn = k;
        nkn = n - k;

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