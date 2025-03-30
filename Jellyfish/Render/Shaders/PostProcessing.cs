namespace Jellyfish.Render.Shaders;

public class PostProcessing : Shader
{
    private readonly RenderTarget _rtColor;

    public PostProcessing(RenderTarget color) : 
        base("shaders/Screenspace.vert", null, "shaders/PostProcessing.frag")
    {
        _rtColor = color;
    }

    public override void Bind()
    {
        base.Bind();
        _rtColor.Bind(0);
    }

    public override void Unload()
    {
        _rtColor.Unload();
        base.Unload();
    }
}