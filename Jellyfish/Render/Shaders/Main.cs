using Jellyfish.Console;
using LightProbe = Jellyfish.Render.Lighting.LightProbe;

namespace Jellyfish.Render.Shaders;

public class Main : Shader
{
    private readonly bool _hasTransparency;
    private readonly bool _hasAlphaTest;
    private readonly TextureRef? _diffuse;
    private readonly TextureRef? _normal;
    private readonly TextureRef? _metRought;
    private readonly TextureRef? _reflectionMap;

    public Main(Material material) : base("shaders/Main.vert", null, "shaders/Main.frag")
    {
        // todo?: use bound material

        if (material.TryGetParam<string>("Diffuse", out var diffusePath))
            _diffuse = Engine.TextureManager.GetTexture(new TextureParams { Path = $"{material.Directory}/{diffusePath}", Srgb = true}).Texture;

        if (material.TryGetParam<string>("Normal", out var normalPath))
            _normal = Engine.TextureManager.GetTexture(new TextureParams { Path = $"{material.Directory}/{normalPath}"}).Texture;

        if (material.TryGetParam<string>("MetalRoughness", out var metroughtPath))
            _metRought = Engine.TextureManager.GetTexture(new TextureParams { Path = $"{material.Directory}/{metroughtPath}"}).Texture;

        _reflectionMap = Engine.TextureManager.GetTexture("_rt_ReflectionsBlurY");

        if (material.TryGetParam<bool>("AlphaTest", out var hasTransparency)) // todo: rename
            _hasTransparency = hasTransparency;

        if (material.TryGetParam<bool>("Cutout", out var alphaTest)) // todo: rename
            _hasAlphaTest = alphaTest;
    }

    public Main(TextureRef diffuse) : base("shaders/Main.vert", null, "shaders/Main.frag")
    {
        _diffuse = diffuse;
    }

    public override void Bind()
    {
        base.Bind();

        SetVector3("cameraPos", Engine.MainViewport.Position);
        SetMatrix4("view", Engine.MainViewport.GetViewMatrix());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());
        SetBool("useNormals", _normal != null);
        SetBool("usePbr", _metRought != null);
        SetBool("useTransparency", _hasTransparency);
        SetBool("useAlphaTest", _hasAlphaTest);
        SetInt("prefilterMips", LightProbe.PrefilterMips);
        SetBool("iblEnabled", ConVarStorage.Get<bool>("mat_ibl_enabled"));
        SetBool("iblPrefilterEnabled", ConVarStorage.Get<bool>("mat_ibl_prefilter"));
        SetBool("sslrEnabled", ConVarStorage.Get<bool>("mat_sslr_enabled"));
        SetVector2("screenSize", Engine.MainViewport.Size);

        Engine.LightManager.LightSourcesSsbo.Bind(0);
        Engine.Renderer.ImageBasedLighting?.LightProbesSsbo.Bind(1);

        BindTexture(0, _diffuse?.Texture);
        BindTexture(1, _normal?.Texture);
        BindTexture(2, _metRought?.Texture);
        BindTexture(3, _reflectionMap?.Texture);
    }

    public override void Unload()
    {
        _diffuse?.Dispose();
        _normal?.Dispose();
        _metRought?.Dispose();
        _reflectionMap?.Dispose();

        base.Unload();
    }
}