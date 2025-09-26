namespace Jellyfish.Render.Shaders;

public class GBuffer : Shader
{
    //private readonly Texture? _diffuse;
    private readonly Texture? _normal;

    public GBuffer(Material material) : base("shaders/Main.vert", null, "shaders/GBuffer.frag")
    {
        //if (material.TryGetParam<string>("Diffuse", out var diffusePath))
        //    _diffuse = Engine.TextureManager.GetTexture(new TextureParams { Name = $"{material.Directory}/{diffusePath}", Srgb = true}).Texture;

        if (material.TryGetParam<string>("Normal", out var normalPath))
            _normal = Engine.TextureManager.GetTexture(new TextureParams { Name = $"{material.Directory}/{normalPath}"}).Texture;
    }

    public override void Bind()
    {
        base.Bind();

        SetMatrix4("view", Engine.MainViewport.GetViewMatrix());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());
        SetBool("hasNormalMap", _normal != null);

        //_diffuse?.Bind(0);
        BindTexture(1, _normal);
    }

    public override void Unload()
    {
        //_diffuse?.Unload();
        _normal?.Unload();

        base.Unload();
    }
}