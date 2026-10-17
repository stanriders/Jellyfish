namespace Jellyfish.Render.Shaders;

public class Downsample : Shader
{

    private readonly TextureRef _rtColor;

    public Downsample(string rtColor) :
        base("shaders/Screenspace.vert", null, "shaders/Downsample.frag")
    {
        _rtColor = Engine.TextureManager.GetTexture(rtColor)!;
    }

    public override void Bind()
    {
        base.Bind();
        BindTexture(0, _rtColor);
    }

    public override void Unload()
    {
        _rtColor.Dispose();
        base.Unload();
    }
}