namespace Jellyfish.Render.Shaders.IBL;

public class Prefiltering : Shader
{
    private readonly TextureRef _rtEnvMap;

    public Prefiltering(TextureRef envMap) :
        base("shaders/Prefiltering.vert", null, "shaders/Prefiltering.frag")
    {
        _rtEnvMap = envMap;
    }
    public override void Bind()
    {
        base.Bind();

        BindTexture(0, _rtEnvMap);

        SetMatrix4("view", Engine.MainViewport.GetViewMatrix().ClearTranslation());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());
    }
}