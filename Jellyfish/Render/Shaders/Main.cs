using Jellyfish.Console;
using LightProbe = Jellyfish.Render.Lighting.LightProbe;

namespace Jellyfish.Render.Shaders;

public class Main() : Shader("shaders/Main.vert", null, "shaders/Main.frag")
{
    private TextureRef? _reflectionMap;

    // todo: predefined shader params that can be edited in a material editor

    public override void Bind()
    {
        base.Bind();

        var diffuse = BoundMaterial?.GetParamTexture("Diffuse", true);
        var normal = BoundMaterial?.GetParamTexture("Normal");
        var metRough = BoundMaterial?.GetParamTexture("MetalRoughness");
        _reflectionMap ??= Engine.TextureManager.GetTexture("_rt_ReflectionsBlurY");

        SetVector3("cameraPos", Engine.MainViewport.Position);
        SetMatrix4("view", Engine.MainViewport.GetViewMatrix());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());

        SetBool("useNormals", normal != null);
        SetBool("usePbr", metRough != null);
        SetBool("useTransparency", BoundMaterial?.GetParam<bool>("AlphaTest") ?? false);  // todo: rename
        SetBool("useAlphaTest", BoundMaterial?.GetParam<bool>("Cutout") ?? false);  // todo: rename

        SetBool("iblEnabled", ConVarStorage.Get<bool>("mat_ibl_enabled"));
        SetBool("iblPrefilterEnabled", ConVarStorage.Get<bool>("mat_ibl_prefilter"));
        SetBool("sslrEnabled", ConVarStorage.Get<bool>("mat_sslr_enabled"));

        SetVector2("screenSize", Engine.MainViewport.Size);
        SetVector2("nearFar",new OpenTK.Mathematics.Vector2(Engine.MainViewport.NearPlane, Engine.MainViewport.FarPlane));

        Engine.LightManager.LightSourcesSsbo.Bind(0);
        ((OpenGLRender)Engine.Renderer).ImageBasedLighting?.LightProbesSsbo.Bind(1);
        Engine.LightManager.ClustersSsbo.Bind(2);

        BindTexture(0, diffuse);
        BindTexture(1, normal);
        BindTexture(2, metRough);
        BindTexture(3, _reflectionMap);
    }

    public override void Unload()
    {
        _reflectionMap?.Dispose();

        base.Unload();
    }
}