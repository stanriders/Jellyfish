using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class ScreenspaceReflections : Shader
{
    private readonly Texture _rtColor;
    private readonly Texture _rtDepth;
    private readonly Texture _rtNormals;

    public ScreenspaceReflections() : 
        base("shaders/Screenspace.vert", null, "shaders/ScreenspaceReflections.frag")
    {
        _rtColor = Engine.TextureManager.GetTexture("_rt_Color")!;
        _rtDepth = Engine.TextureManager.GetTexture("_rt_Depth")!;
        _rtNormals = Engine.TextureManager.GetTexture("_rt_Normal")!;
    }

    public override void Bind()
    {
        base.Bind();

        SetMatrix4("uProjection", Engine.MainViewport.GetProjectionMatrix());
        SetVector2("uCameraParams", new Vector2(Engine.MainViewport.NearPlane, Engine.MainViewport.FarPlane));

        BindTexture(0, _rtColor);
        BindTexture(1, _rtDepth);
        BindTexture(2, _rtNormals);
    }

    public override void Unload()
    {
        _rtColor.Unload();
        _rtDepth.Unload();
        _rtNormals.Unload();

        base.Unload();
    }
}

