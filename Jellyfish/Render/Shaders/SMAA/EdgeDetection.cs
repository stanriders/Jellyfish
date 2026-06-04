
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders.SMAA;

public class EdgeDetection : Shader
{
    private readonly Texture _rtColor;
    public EdgeDetection() 
        : base("shaders/EdgeDetection.vert", null, "shaders/EdgeDetection.frag")
    {
        _rtColor = Engine.TextureManager.GetTexture("_rt_Combined")!;
    }

    public override void Bind()
    {
        base.Bind();
        BindTexture(0, _rtColor);
        SetVector2("uTexelSize", 1.0f / (Vector2)Engine.MainViewport.Size);
    }

    public override void Unload()
    {
        _rtColor.Unload();

        base.Unload();
    }
}