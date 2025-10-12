namespace Jellyfish.Render.Shaders.IBL;

public class Irradiance : Shader
{
    private readonly Texture _rtEnvMap;

    public Irradiance(Texture envMap) :
        base("shaders/Irradiance.vert", null, "shaders/Irradiance.frag")
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

    public override void Unload()
    {
        _rtEnvMap.Unload();
        base.Unload();
    }
}