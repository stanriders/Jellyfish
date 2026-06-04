
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders.SMAA;

public class NeighborhoodBlending : Shader
{
    private readonly Texture _rtColor;
    private readonly Texture _rtBlend;
    public NeighborhoodBlending() 
        : base("shaders/NeighborhoodBlending.vert", null, "shaders/NeighborhoodBlending.frag")
    {
        _rtColor = Engine.TextureManager.GetTexture("_rt_Combined")!;
        _rtBlend = Engine.TextureManager.GetTexture("_rt_SMAABlendingWeightCalculation")!;
    }

    public override void Bind()
    {
        base.Bind();
        BindTexture(0, _rtColor);
        BindTexture(1, _rtBlend);

        SetVector2("uViewportSize", Engine.MainViewport.Size);
        SetVector2("uTexelSize", 1.0f / (Vector2)Engine.MainViewport.Size);
    }

    public override void Unload()
    {
        _rtColor.Unload();
        _rtBlend.Unload();

        base.Unload();
    }
}