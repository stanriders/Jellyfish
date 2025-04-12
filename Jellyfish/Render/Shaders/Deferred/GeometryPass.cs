using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Shaders.Deferred;

public class GeometryPass : Shader
{
    private readonly Texture _diffuse;
    private readonly Texture? _normal;

    public GeometryPass(string diffusePath, string? normalPath = null) : base("shaders/Main.vert", null, "shaders/GeometryPass.frag")
    {
        _diffuse = TextureManager.GetTexture(diffusePath, TextureTarget.Texture2d, true).Texture;

        if (!string.IsNullOrEmpty(normalPath))
        {
            _normal = TextureManager.GetTexture(normalPath, TextureTarget.Texture2d, false).Texture;
        }
    }

    public override void Bind()
    {
        base.Bind();

        SetMatrix4("view", Camera.Instance.GetViewMatrix());
        SetMatrix4("projection", Camera.Instance.GetProjectionMatrix());

        _diffuse.Bind(0);
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
        _diffuse.Unload();
        _normal?.Unload();

        base.Unload();
    }
}