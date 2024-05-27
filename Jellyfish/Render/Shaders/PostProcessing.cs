using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Shaders;

public class PostProcessing : Shader
{
    private readonly RenderTarget _rtColor;
    private readonly RenderTarget _rtDepth;

    public PostProcessing(RenderTarget color, RenderTarget depth) : 
        base("shaders/PostProcessing.vert", null, "shaders/PostProcessing.frag")
    {
        _rtColor = color;
        _rtDepth = depth;
    }

    public override void Bind()
    {
        base.Bind();

        GL.ActiveTexture(TextureUnit.Texture0);
        _rtColor.Bind();

        GL.ActiveTexture(TextureUnit.Texture1);
        _rtDepth.Bind();
    }
}