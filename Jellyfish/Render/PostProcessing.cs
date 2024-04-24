using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class PostProcessing
{
    private readonly Shaders.PostProcessing _shader;
    private readonly VertexArray _vertexArray;

    private readonly float[] _quad = {
        // positions   // texCoords
        -1.0f,  1.0f,  0.0f, 1.0f,
        -1.0f, -1.0f,  0.0f, 0.0f,
         1.0f, -1.0f,  1.0f, 0.0f,

        -1.0f,  1.0f,  0.0f, 1.0f,
         1.0f, -1.0f,  1.0f, 0.0f,
         1.0f,  1.0f,  1.0f, 1.0f
    };

    public PostProcessing(FrameBuffer frameBuffer)
    {
        var vertexBuffer = GL.GenBuffer();

        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _quad.Length * sizeof(float), _quad, BufferUsageHint.StaticDraw);

        _vertexArray = new VertexArray();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);

        _shader = new Shaders.PostProcessing(frameBuffer.FramebufferTextureHandle);
        _shader.Bind();
    }

    public void Draw()
    {
        GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);

        _shader.Bind();

        _vertexArray.Bind();
        
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        GL.BindVertexArray(0);
        GL.UseProgram(0);
    }
}