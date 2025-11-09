namespace Jellyfish.Render.Shaders;

public class Bloom : Shader
{
    private readonly string _rtColorName;
    private readonly float _filterSize;
    private Texture? _rtColor;

    public Bloom(string rtColorName, float filterSize) :
        base("shaders/Screenspace.vert", null, "shaders/Bloom.frag")
    {
        _rtColorName = rtColorName;
        _filterSize = filterSize;
        _rtColor = Engine.TextureManager.GetTexture(_rtColorName)!;
    }

    public override void Bind()
    {
        base.Bind();
        if (_rtColor == null)
            _rtColor = Engine.TextureManager.GetTexture(_rtColorName)!;

        SetFloat("filterRadius", _filterSize);

        BindTexture(0, _rtColor);
    }

    public override void Unload()
    {
        _rtColor?.Unload();
        base.Unload();
    }
}