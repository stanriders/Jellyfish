using Jellyfish.Debug;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using Jellyfish.Utils;

namespace Jellyfish.Render;

public class FinalOut
{
    private readonly Shaders.SimpleOut _shader;

    public FinalOut()
    {
        _shader = new Shaders.SimpleOut(Engine.TextureManager.GetTexture("_rt_SMAANeighborhoodBlending")!);
    }

    public void Draw()
    {
        var stopwatch = Stopwatch.StartNew();
        GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);

        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        _shader.Bind();
        CommonShapes.DrawQuad();
        _shader.Unbind();

        PerformanceMeasurment.Add("FinalOut.Draw", stopwatch.Elapsed.TotalMilliseconds);
    }

    public void Unload()
    {
        _shader.Unload();
    }
}