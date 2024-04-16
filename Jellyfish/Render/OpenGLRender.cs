using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class OpenGLRender : IRender
{
    public OpenGLRender()
    {
        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthFunc(DepthFunction.Less);

        GL.Enable(EnableCap.CullFace);
    }

    public void Frame()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        MeshManager.Draw();
    }

    public void Unload()
    {
        MeshManager.Unload();
    }
}