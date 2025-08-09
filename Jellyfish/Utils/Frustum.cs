
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;
using System;
using System.Linq;

namespace Jellyfish.Utils
{
    public readonly struct Frustum
    {
        public Vector4[] Planes { get; }
        public Vector3[] Corners { get; }
        public Vector3[] NearCorners => Corners[..4];
        public Vector3[] FarCorners => Corners[4..];
        public Vector3 Center { get; }
        public Vector3 NearPlaneCenter { get; }
        public Vector3 FarPlaneCenter { get; }

        private static readonly Vector3[] ClipCorners =
        [
            new(-1, -1, -1),
            new(+1, -1, -1),
            new(-1, +1, -1),
            new(+1, +1, -1),
            new(-1, -1, +1),
            new(+1, -1, +1),
            new(-1, +1, +1),
            new(+1, +1, +1),
        ];

        public Frustum(Matrix4 viewProjectionMatrix)
        {
            Planes = new Vector4[6];

            float m00 = viewProjectionMatrix.M11, m01 = viewProjectionMatrix.M12, m02 = viewProjectionMatrix.M13, m03 = viewProjectionMatrix.M14;
            float m10 = viewProjectionMatrix.M21, m11 = viewProjectionMatrix.M22, m12 = viewProjectionMatrix.M23, m13 = viewProjectionMatrix.M24;
            float m20 = viewProjectionMatrix.M31, m21 = viewProjectionMatrix.M32, m22 = viewProjectionMatrix.M33, m23 = viewProjectionMatrix.M34;
            float m30 = viewProjectionMatrix.M41, m31 = viewProjectionMatrix.M42, m32 = viewProjectionMatrix.M43, m33 = viewProjectionMatrix.M44;

            // LEFT   plane
            Planes[0] = new Vector4(m03 + m00, m13 + m10, m23 + m20, m33 + m30);
            // RIGHT  plane
            Planes[1] = new Vector4(m03 - m00, m13 - m10, m23 - m20, m33 - m30);
            // BOTTOM plane
            Planes[2] = new Vector4(m03 + m01, m13 + m11, m23 + m21, m33 + m31);
            // TOP    plane
            Planes[3] = new Vector4(m03 - m01, m13 - m11, m23 - m21, m33 - m31);
            // NEAR   plane
            Planes[4] = new Vector4(m03 + m02, m13 + m12, m23 + m22, m33 + m32);
            // FAR    plane
            Planes[5] = new Vector4(m03 - m02, m13 - m12, m23 - m22, m33 - m32);

            for (var i = 0; i < 6; i++)
            {
                var length = (float)Math.Sqrt(
                    Planes[i].X * Planes[i].X +
                    Planes[i].Y * Planes[i].Y +
                    Planes[i].Z * Planes[i].Z
                );
                Planes[i].X /= length;
                Planes[i].Y /= length;
                Planes[i].Z /= length;
                Planes[i].W /= length;
            }

            Matrix4.Invert(viewProjectionMatrix, out var invViewProj);

            Corners = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                // Make it a 4D vector with w = 1
                var corner4 = new Vector4(ClipCorners[i].X, ClipCorners[i].Y, ClipCorners[i].Z, 1.0f);

                // Transform by inverse view-projection
                Vector4 transformed = corner4 * invViewProj;

                // Perspective divide
                float invW = 1f / transformed.W;
                Corners[i] = new Vector3(transformed.X * invW,
                    transformed.Y * invW,
                    transformed.Z * invW);
            }

            Center = Corners.Average();
            NearPlaneCenter = NearCorners.Average();
            FarPlaneCenter = FarCorners.Average();
        }

        public bool IsInside(Vector3 center, float radius)
        {
            foreach (var plane in Planes)
            {
                // Distance from plane to sphere center:
                var distance = plane.X * center.X + plane.Y * center.Y + plane.Z * center.Z + plane.W;

                // If the center is more negative than -radius => completely outside
                if (distance < -radius)
                    return false;
            }

            return true;
        }

        public bool IsInside(BoundingBox box)
        {
            foreach (var (a, b, c, d) in Planes)
            {
                var px = (a >= 0) ? box.Max.X : box.Min.X;
                var py = (b >= 0) ? box.Max.Y : box.Min.Y;
                var pz = (c >= 0) ? box.Max.Z : box.Min.Z;

                // Distance of that corner to the plane
                var dist = (a * px) + (b * py) + (c * pz) + d;

                // If "most positive" corner is behind plane, entire box is behind plane
                if (dist < 0f)
                    return false;
            }

            // If not behind any plane => it’s at least partially in frustum
            return true;
        }
    }
}
