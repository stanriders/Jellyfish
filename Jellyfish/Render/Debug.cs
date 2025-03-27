
using ImGuiNET;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public static class Debug
{
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
}