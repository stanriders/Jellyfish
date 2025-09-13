using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Shaders.IBL;

public class Irradiance : Shader
{
    private readonly Texture _rtEnvMap;

    public Irradiance() :
        base("shaders/Irradiance.vert", null, "shaders/Irradiance.frag")
    {
        _rtEnvMap = Engine.TextureManager.GetTexture("_rt_EnvironmentMap")!;
    }
    public override void Bind()
    {
        base.Bind();

        _rtEnvMap.Bind(0);

        SetMatrix4("view", Engine.MainViewport.GetViewMatrix().ClearTranslation());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());
    }

    public override void Unbind()
    {
        GL.BindTextureUnit(0, 0);
        base.Unbind();
    }

    public override void Unload()
    {
        _rtEnvMap.Unload();
        base.Unload();
    }
}