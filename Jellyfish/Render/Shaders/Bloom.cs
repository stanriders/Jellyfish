using Jellyfish.Console;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class Bloom : Shader
{

    private readonly Texture _rtColor;

    public Bloom() :
        base("shaders/Screenspace.vert", null, "shaders/Bloom.frag")
    {
        _rtColor = Engine.TextureManager.GetTexture("_rt_Color")!;
    }

    public override void Bind()
    {
        base.Bind();

        SetFloat("threshold", ConVarStorage.Get<float>("mat_bloom_threshold"));
        BindTexture(0, _rtColor);
    }

    public override void Unload()
    {
        _rtColor.Unload();
        base.Unload();
    }
}