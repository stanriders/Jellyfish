using System.Collections.Generic;
using ImGuiNET;
using Jellyfish.Utils;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public static class DebugRender
{
    public static void DrawBoundingBox(Vector3 position, BoundingBox box)
    {
        RenderScheduler.Schedule(() =>
        {
            var drawList = ImGui.GetBackgroundDrawList();

            var max = box.Max;
            var min = box.Min;

            var c1 = new Vector3(max.X, max.Y, max.Z);
            var c2 = new Vector3(max.X, max.Y, min.Z);
            var c3 = new Vector3(min.X, max.Y, max.Z);
            var c4 = new Vector3(min.X, max.Y, min.Z);
            var c5 = new Vector3(max.X, min.Y, max.Z);
            var c6 = new Vector3(max.X, min.Y, min.Z);
            var c7 = new Vector3(min.X, min.Y, max.Z);
            var c8 = new Vector3(min.X, min.Y, min.Z);

            var lines = new List<Vector3[]>();
            lines.Add([c1, c2]);
            lines.Add([c2, c4]);
            lines.Add([c4, c3]);
            lines.Add([c3, c1]);

            lines.Add([c5, c6]);
            lines.Add([c6, c8]);
            lines.Add([c8, c7]);
            lines.Add([c7, c5]);

            lines.Add([c1, c5]);
            lines.Add([c2, c6]);
            lines.Add([c3, c7]);
            lines.Add([c4, c8]);

            foreach (var line in lines)
            {
                var start = (line[0] + position).ToNumericsVector().ToScreenspace();
                var end = (line[1] + position).ToNumericsVector().ToScreenspace();

                drawList.AddLine(start, end, uint.MaxValue);
            }
        });
    }

    public static void DrawText(Vector3 position, string text)
    {
        RenderScheduler.Schedule(() =>
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var screenspacePosition = position.ToNumericsVector().ToScreenspace();

            drawList.AddText(screenspacePosition, uint.MaxValue, text);
        });
    }

    public static void DrawLine(Vector3 start, Vector3 end)
    {
        RenderScheduler.Schedule(() =>
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var startScreenspace = start.ToNumericsVector().ToScreenspace();
            var endScreenspace = end.ToNumericsVector().ToScreenspace();

            drawList.AddLine(startScreenspace, endScreenspace, uint.MaxValue);
        });
    }

    public static void DrawFrustum(Frustum frustum)
    {
        RenderScheduler.Schedule(() =>
        {
            var drawList = ImGui.GetBackgroundDrawList();

            var lines = new List<Vector3[]>();

            lines.Add([frustum.NearCorners[0], frustum.NearCorners[1]]);
            lines.Add([frustum.NearCorners[1], frustum.NearCorners[3]]);
            lines.Add([frustum.NearCorners[3], frustum.NearCorners[2]]);
            lines.Add([frustum.NearCorners[2], frustum.NearCorners[0]]);

            lines.Add([frustum.FarCorners[0], frustum.FarCorners[1]]);
            lines.Add([frustum.FarCorners[1], frustum.FarCorners[3]]);
            lines.Add([frustum.FarCorners[3], frustum.FarCorners[2]]);
            lines.Add([frustum.FarCorners[2], frustum.FarCorners[0]]);

            // sides
            lines.Add([frustum.NearCorners[0], frustum.FarCorners[0]]);
            lines.Add([frustum.NearCorners[1], frustum.FarCorners[1]]);
            lines.Add([frustum.NearCorners[2], frustum.FarCorners[2]]);
            lines.Add([frustum.NearCorners[3], frustum.FarCorners[3]]);

            foreach (var line in lines)
            {
                var start = line[0].ToNumericsVector().ToScreenspace();
                var end = line[1].ToNumericsVector().ToScreenspace();

                drawList.AddLine(start, end, uint.MaxValue);
            }
        });
    }
}