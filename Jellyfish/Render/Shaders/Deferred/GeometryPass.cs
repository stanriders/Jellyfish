using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Shaders.Deferred;

public class GeometryPass : Shader
{
    private readonly Texture? _diffuse;
    private readonly Texture? _normal;

    public GeometryPass(Material material) : base("shaders/Main.vert", null, "shaders/GeometryPass.frag")
    {
        if (material.TryGetParam<string>("Diffuse", out var diffusePath))
            _diffuse = Engine.TextureManager.GetTexture($"{material.Directory}/{diffusePath}", TextureTarget.Texture2d, true).Texture;

        if (material.TryGetParam<string>("Normal", out var normalPath))
            _normal = Engine.TextureManager.GetTexture($"{material.Directory}/{normalPath}", TextureTarget.Texture2d, false).Texture;
    }

    public override void Bind()
    {
        base.Bind();

        SetMatrix4("view", Engine.MainViewport.GetViewMatrix());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());
        SetBool("hasNormalMap", _normal != null);

        _diffuse?.Bind(0);
        _normal?.Bind(1);
    }

    public override void Unbind()
    {
        GL.BindTextureUnit(0, 0);
        GL.BindTextureUnit(1, 0);

        base.Unbind();
    }

    public override void Unload()
    {
        _diffuse?.Unload();
        _normal?.Unload();

        base.Unload();
    }
}