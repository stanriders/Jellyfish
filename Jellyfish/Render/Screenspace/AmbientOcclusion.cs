
using Jellyfish.Console;
using Jellyfish.Debug;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Jellyfish.Render.Screenspace;

public class GtaoEnabled() : ConVar<bool>("mat_gtao_enabled", true);
public class GtaoQuality() : ConVar<int>("mat_gtao_quality", 2);
public class GtaoRadius() : ConVar<float>("mat_gtao_radius", 1.0f);
public class GtaoIntensity() : ConVar<float>("mat_gtao_intensity", 1.0f);
public class GtaoThickness() : ConVar<float>("mat_gtao_thickness", 5.0f);

public class AmbientOcclusion : ScreenspaceEffect
{
    public AmbientOcclusion() : base("Gtao", SizedInternalFormat.Rgb16f, new Shaders.AmbientOcclusion())
    {
    }

    public override void Draw()
    {
        var stopwatch = Stopwatch.StartNew();
        if (!ConVarStorage.Get<bool>("mat_gtao_enabled"))
        {
            Buffer.Bind(FramebufferTarget.DrawFramebuffer);

            GL.ClearColor(Color4.White);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            Buffer.Unbind();
            PerformanceMeasurment.Add("AmbientOcclusion.Draw", stopwatch.Elapsed.TotalMilliseconds);
            return;
        }

        base.Draw();
        PerformanceMeasurment.Add("AmbientOcclusion.Draw", stopwatch.Elapsed.TotalMilliseconds);
    }
}