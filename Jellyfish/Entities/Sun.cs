using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfish.Render;
using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("light_sun")]
public class Sun : LightEntity
{
    public override float NearPlane => 1f;
    public override float FarPlane => 10000f;
    public override int ShadowResolution => 4096;

    public const int cascades = 4;

    public static (int Near, int Far)[] CascadeRanges =
    [
        (1, 200),
        (200, 1000),
        (1000, 3000),
        (3000, 10000)
    ];

    public override Matrix4[] Projections
    {
        get
        {
            var projections = new List<Matrix4>(cascades);

            for (var i = 0; i < cascades; i++)
            {
                var frustum = Camera.Instance.GetFrustum(CascadeRanges[i].Near, CascadeRanges[i].Far);

                var center = frustum.Corners.Aggregate(new Vector3(0, 0, 0), (current, v) => current + v) / frustum.Corners.Length;

                var direction = Vector3.Transform(Vector3.UnitY, Rotation).Normalized();

                var lightView = Matrix4.LookAt(center + direction, center, Vector3.UnitY);

                var minX = float.MaxValue;
                var maxX = float.MinValue;
                var minY = float.MaxValue;
                var maxY = float.MinValue;
                var minZ = float.MaxValue;
                var maxZ = float.MinValue;

                foreach (var v in frustum.Corners)
                {
                    var trf = Vector3.TransformPosition(v, lightView);
                    minX = Math.Min(minX, trf.X);
                    maxX = Math.Max(maxX, trf.X);
                    minY = Math.Min(minY, trf.Y);
                    maxY = Math.Max(maxY, trf.Y);
                    minZ = Math.Min(minZ, trf.Z);
                    maxZ = Math.Max(maxZ, trf.Z);
                }

                // Tune this parameter according to the scene
                const float zMult = 10.0f;
                if (minZ < 0)
                {
                    minZ *= zMult;
                }
                else
                {
                    minZ /= zMult;
                }
                if (maxZ < 0)
                {
                    maxZ /= zMult;
                }
                else
                {
                    maxZ *= zMult;
                }

                var lightProjection = Matrix4.CreateOrthographicOffCenter(minX, maxX, minY, maxY, minZ, maxZ);
                projections.Add(lightView * lightProjection);
            }

            return projections.ToArray();
        }
    }
}