using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Screenspace;

public abstract class ScreenspaceEffect
{
    protected readonly FrameBuffer Buffer;
    protected readonly Texture RenderTarget;
    protected readonly Shader Shader;
    protected readonly VertexArray VertexArray;
    protected readonly VertexBuffer VertexBuffer;

    protected ScreenspaceEffect(string rtName, SizedInternalFormat format, Shader shader)
    {
        Shader = shader;

        Buffer = new FrameBuffer();
        Buffer.Bind();

        RenderTarget = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = $"_rt_{rtName}",
            WrapMode = TextureWrapMode.ClampToEdge,
            MinFiltering = TextureMinFilter.Nearest,
            MagFiltering = TextureMagFilter.Nearest,
            RenderTargetParams = new RenderTargetParams
            {
                Width = Engine.MainViewport.Size.X,
                Heigth = Engine.MainViewport.Size.Y,
                InternalFormat = format,
                Attachment = FramebufferAttachment.ColorAttachment0,
            }
        });

        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

        Buffer.Check();
        Buffer.Unbind();

        VertexBuffer = new VertexBuffer($"Screenspace_{rtName}", CommonShapes.Quad);
        VertexArray = new VertexArray(VertexBuffer, null, 4 * sizeof(float));

        var vertexLocation = Shader.GetAttribLocation("aPos");
        if (vertexLocation != null)
        {
            GL.EnableVertexArrayAttrib(VertexArray.Handle, vertexLocation.Value);
            GL.VertexArrayAttribFormat(VertexArray.Handle, vertexLocation.Value, 2, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(VertexArray.Handle, vertexLocation.Value, 0);
        }

        var texCoordLocation = Shader.GetAttribLocation("aTexCoords");
        if (texCoordLocation != null)
        {
            GL.EnableVertexArrayAttrib(VertexArray.Handle, texCoordLocation.Value);
            GL.VertexArrayAttribFormat(VertexArray.Handle, texCoordLocation.Value, 2, VertexAttribType.Float, false,
                2 * sizeof(float));
            GL.VertexArrayAttribBinding(VertexArray.Handle, texCoordLocation.Value, 0);
        }
    }

    public virtual void Draw()
    {
        Buffer.Bind(FramebufferTarget.DrawFramebuffer);

        GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        Shader.Bind();
        VertexArray.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        PerformanceMeasurment.Increment("DrawCalls");

        VertexArray.Unbind();
        Shader.Unbind();

        Buffer.Unbind();
    }

    public virtual void Unload()
    {
        Buffer.Unload();
        RenderTarget.Unload();
        Shader.Unload();
        VertexArray.Unload();
        VertexBuffer.Unload();
    }
}