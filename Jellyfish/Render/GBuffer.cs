using System.Collections.Generic;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class GBuffer
{
    private readonly List<RenderTarget> _renderTargets = new();
    private readonly FrameBuffer _buffer;

    public GBuffer(RenderTarget depthRenderTarget)
    {
        _buffer = new FrameBuffer();
        _buffer.Bind();

        for (uint i = 0; i < (uint)GBufferType.Count; i++)
        {
            // diffuse is special because we want to pass alpha too
            var format = (GBufferType)i == GBufferType.Diffuse
                ? SizedInternalFormat.Rgba16f
                : SizedInternalFormat.Rgb16f;

            _renderTargets.Add(new RenderTarget($"_rt_{(GBufferType)i}", MainWindow.WindowWidth, MainWindow.WindowHeight, format, FramebufferAttachment.ColorAttachment0 + i, 
                TextureWrapMode.ClampToEdge));
        }

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, depthRenderTarget.TextureHandle, 0);

        GL.DrawBuffers(4, new[] { DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1, DrawBufferMode.ColorAttachment2, DrawBufferMode.ColorAttachment3 });

        _buffer.Check();
        _buffer.Unbind();
    }

    public void GeometryPass()
    {
        _buffer.Bind(FramebufferTarget.DrawFramebuffer);

        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        MeshManager.DrawGBuffer();

        _buffer.Unbind();
    }

    public void BindForReading()
    {
        _buffer.Bind(FramebufferTarget.ReadFramebuffer);
    }

    public void SetReadBuffer(GBufferType type)
    {
        GL.ReadBuffer(ReadBufferMode.ColorAttachment0 + (uint)type);
    }

    public void Unbind()
    {
        _buffer.Unbind();
    }

    public void Unload()
    {
        foreach (var renderTarget in _renderTargets)
        {
            renderTarget.Unload();
        }

        _buffer.Unload();
    }
}

public enum GBufferType
{
    Position, 
    Diffuse,
    Normal,
    Texcoord,

    Count
}