using System.Collections.Generic;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render;

public class GBuffer
{
    private readonly List<Texture> _renderTargets = new();
    private readonly FrameBuffer _buffer;

    public GBuffer(Texture depthRenderTarget)
    {
        _buffer = new FrameBuffer();
        _buffer.Bind();

        for (uint i = 0; i < (uint)GBufferType.Count; i++)
        {
            // diffuse is special because we want to pass alpha too
            /*var format = (GBufferType)i == GBufferType.Diffuse
                ? SizedInternalFormat.Rgba16f
                : SizedInternalFormat.Rgb16f;
            */

            var texture = Engine.TextureManager.CreateTexture(new RenderTargetParams
            {
                Width = Engine.MainViewport.Size.X,
                Heigth = Engine.MainViewport.Size.Y,
                TextureParams = new TextureParams
                {
                    Name = $"_rt_{(GBufferType)i}",
                    WrapMode = TextureWrapMode.ClampToEdge,
                    MinFiltering = TextureMinFilter.Nearest,
                    MagFiltering = TextureMagFilter.Nearest,
                    InternalFormat = SizedInternalFormat.Rgb16f
                }
            });

            _buffer.AttachTexture(texture.Handle, attachment: FramebufferAttachment.ColorAttachment0 + i);
            _buffer.DrawInto(ColorBuffer.ColorAttachment0 + i);

            _renderTargets.Add(texture);
        }

        _buffer.AttachTexture(depthRenderTarget.Handle, attachment: FramebufferAttachment.DepthAttachment);

        _buffer.Check();
        _buffer.Unbind();
    }

    public void GeometryPass()
    {
        _buffer.Bind(FramebufferTarget.DrawFramebuffer);

        GL.Viewport(0, 0, Engine.MainViewport.Size.X, Engine.MainViewport.Size.Y);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Engine.MeshManager.DrawGBuffer();

        _buffer.Unbind();
    }

    public void BindForReading()
    {
        _buffer.Bind(FramebufferTarget.ReadFramebuffer);
    }

    public void SetReadBuffer(GBufferType type)
    {
        _buffer.ReadFrom(ColorBuffer.ColorAttachment0 + (uint)type);
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

        _buffer.Dispose();
    }
}

public enum GBufferType
{
    //Position, 
    //Diffuse,
    Normal,
    //Texcoord,

    Count
}