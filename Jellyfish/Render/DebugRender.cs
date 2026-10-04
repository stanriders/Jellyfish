using System;
using System.Collections.Generic;
using Hexa.NET.ImGui;
using Jellyfish.Utils;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public static class DebugRender
{
    public static void DrawBoundingBox(Vector3 position, BoundingBox box)
    {
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
            DrawLine(line[0] + position, line[1] + position);
    }

    public static void DrawText(Vector3 position, string text)
    {
        Scheduler.RenderSchedule(() =>
        {
            var drawList = ImGui.GetBackgroundDrawList();
            var screenspacePosition = position.ToNumericsVector().ToScreenspace();

            drawList.AddText(screenspacePosition, uint.MaxValue, text);
        });
    }

    public static void DrawLine(Vector3 start, Vector3 end, uint color = uint.MaxValue)
    {
        Scheduler.RenderSchedule(() =>
        {
            var view = Engine.MainViewport.GetViewMatrix();
            var near = -Engine.MainViewport.NearPlane;
            var startDepth = (new Vector4(start, 1.0f) * view).Z;
            var endDepth = (new Vector4(end, 1.0f) * view).Z;

            if (startDepth > near && endDepth > near)
                return;

            if (startDepth > near)
                start = Vector3.Lerp(start, end, (startDepth - near) / (startDepth - endDepth));
            else if (endDepth > near)
                end = Vector3.Lerp(end, start, (endDepth - near) / (endDepth - startDepth));

            var drawList = ImGui.GetBackgroundDrawList();
            var startScreenspace = start.ToNumericsVector().ToScreenspace();
            var endScreenspace = end.ToNumericsVector().ToScreenspace();

            drawList.AddLine(startScreenspace, endScreenspace, color);
        });
    }

    public static void DrawFrustum(Frustum frustum)
    {
        Scheduler.RenderSchedule(() =>
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

        var normal1 = MathUtils.CalculateNormal(frustum.NearCorners[0], frustum.NearCorners[1], frustum.NearCorners[3]);
        var normal2 = MathUtils.CalculateNormal(frustum.NearCorners[2], frustum.NearCorners[0], frustum.NearCorners[3]);
        var normal3 = MathUtils.CalculateNormal(frustum.FarCorners[0], frustum.FarCorners[1], frustum.FarCorners[3]);
        var normal4 = MathUtils.CalculateNormal(frustum.FarCorners[2], frustum.FarCorners[0], frustum.FarCorners[3]);

        DrawMesh(new Mesh($"frustum_{Random.Shared.Next()}", 
        [
            new() { Coordinates = frustum.NearCorners[0], Normal = normal1, UV = new Vector2(0, 0) },
            new() { Coordinates = frustum.NearCorners[1], Normal = normal1, UV = new Vector2(1, 0) },
            new() { Coordinates = frustum.NearCorners[3], Normal = normal1, UV = new Vector2(1, 1) },

            new() { Coordinates = frustum.NearCorners[2], Normal = normal2, UV = new Vector2(0, 1) },
            new() { Coordinates = frustum.NearCorners[0], Normal = normal2, UV = new Vector2(0, 0) },
            new() { Coordinates = frustum.NearCorners[3], Normal = normal2, UV = new Vector2(1, 1) },

            new() { Coordinates = frustum.FarCorners[0], Normal = normal3, UV = new Vector2(0, 0) },
            new() { Coordinates = frustum.FarCorners[1], Normal = normal3, UV = new Vector2(1, 0) },
            new() { Coordinates = frustum.FarCorners[3], Normal = normal3, UV = new Vector2(1, 1) },

            new() { Coordinates = frustum.FarCorners[2], Normal = normal4, UV = new Vector2(0, 1) },
            new() { Coordinates = frustum.FarCorners[0], Normal = normal4, UV = new Vector2(0, 0) },
            new() { Coordinates = frustum.FarCorners[3], Normal = normal4, UV = new Vector2(1, 1) },
        ], texture: "materials/error.mat") {IsDev = true});
    }

    public static void DrawMesh(Mesh mesh)
    {
        Scheduler.RenderSchedule(() =>
        {
            Engine.MeshManager.AddMesh(mesh, true);
        });
    }
}