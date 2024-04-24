using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
namespace Jellyfish.Render;

public class OpenGLRender : IRender
{
    private readonly PostProcessing _postProcessing;
    private readonly FrameBuffer _mainFramebuffer = new();

    public OpenGLRender()
    {
        GL.Enable(EnableCap.CullFace);
        _postProcessing = new PostProcessing(_mainFramebuffer);
    }

    public void Frame()
    {
        _mainFramebuffer.Bind();

        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        GL.Viewport(0, 0, MainWindow.WindowWidth, MainWindow.WindowHeight);
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        MeshManager.Draw();

        _mainFramebuffer.Unbind();

        _postProcessing.Draw();
    }

    public void Unload()
    {
        MeshManager.Unload();
    }
}