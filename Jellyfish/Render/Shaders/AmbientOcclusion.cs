
using Jellyfish.Console;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class AmbientOcclusion : Shader
{
    private readonly TextureRef _rtDepth;
    private readonly TextureRef _rtNormals;

    public AmbientOcclusion() : 
        base("shaders/Screenspace.vert", null, "shaders/AmbientOcclusion.frag")
    {
        _rtDepth = Engine.TextureManager.GetTexture("_rt_Depth")!;
        _rtNormals = Engine.TextureManager.GetTexture("_rt_Normal")!;
    }
    public override void Bind()
    {
        base.Bind();

        BindTexture(0, _rtDepth);
        BindTexture(1, _rtNormals);

        SetVector2("screenSize", _rtDepth.Texture.Size);
        SetVector3("cameraParams", new Vector3(Engine.MainViewport.Fov, Engine.MainViewport.NearPlane, Engine.MainViewport.FarPlane));
        SetVector4("gtaoParams", new Vector4(ConVarStorage.Get<int>("mat_gtao_quality"), ConVarStorage.Get<float>("mat_gtao_radius"), ConVarStorage.Get<float>("mat_gtao_intensity"), ConVarStorage.Get<float>("mat_gtao_thickness")));
    }

    public override void Unload()
    {
        _rtDepth.Dispose();
        _rtNormals.Dispose();

        base.Unload();
    }
}