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

        _rtColor.Bind(0);
        _rtDepth.Bind(1);
    }

    public override void Unload()
    {
        _rtColor.Unload();
        _rtDepth.Unload();

        base.Unload();
    }
}