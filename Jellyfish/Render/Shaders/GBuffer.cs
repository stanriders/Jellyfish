namespace Jellyfish.Render.Shaders;

public class GBuffer : Shader
{
    private readonly Texture? _diffuse;
    private readonly Texture? _normal;
    private readonly bool _alphaTest;

    public GBuffer(Material material) : base("shaders/Main.vert", null, "shaders/GBuffer.frag")
    {
        if (material.TryGetParam<string>("Diffuse", out var diffusePath))
            _diffuse = Engine.TextureManager.GetTexture(new TextureParams { Path = $"{material.Directory}/{diffusePath}", Srgb = true}).Texture;

        if (material.TryGetParam<string>("Normal", out var normalPath))
            _normal = Engine.TextureManager.GetTexture(new TextureParams { Path = $"{material.Directory}/{normalPath}"}).Texture;

        if (material.TryGetParam<bool>("Cutout", out var alphaTest)) // todo: rename
            _alphaTest = alphaTest;
    }

    public override void Bind()
    {
        base.Bind();

        SetMatrix4("view", Engine.MainViewport.GetViewMatrix());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());
        SetBool("hasNormalMap", _normal != null);
        SetBool("hasAlphaTest", _alphaTest);

        BindTexture(0, _diffuse);
        BindTexture(1, _normal);
    }

    public override void Unload()
    {
        _diffuse?.Unload();
        _normal?.Unload();

        base.Unload();
    }
}