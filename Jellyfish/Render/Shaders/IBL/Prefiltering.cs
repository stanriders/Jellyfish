namespace Jellyfish.Render.Shaders.IBL;

public class Prefiltering() : Shader("shaders/Prefiltering.vert", null, "shaders/Prefiltering.frag")
{
    private TextureRef? _rtEnvMap;

    public void SetEnvMap(TextureRef envMap)
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