
using OpenTK.Graphics.OpenGL;
using System;

namespace Jellyfish.Render.Shaders;

public class PostProcessing : Shader
{
    private readonly int _framebufferTextureHandle;

    public PostProcessing(int framebufferTextureHandle) : base("shaders/PostProcessing.vert", null, "shaders/PostProcessing.frag")
    {
        _framebufferTextureHandle = framebufferTextureHandle;

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _framebufferTextureHandle);
        
        var vertexLocation = GetAttribLocation("aPos");
        GL.EnableVertexAttribArray(vertexLocation);
        GL.VertexAttribPointer(vertexLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), IntPtr.Zero);

        var uvLocation = GetAttribLocation("aTexCoords");
        GL.EnableVertexAttribArray(uvLocation);
        GL.VertexAttribPointer(uvLocation, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
    }

    public override void Bind()
    {
        base.Bind();

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _framebufferTextureHandle);
    }
}