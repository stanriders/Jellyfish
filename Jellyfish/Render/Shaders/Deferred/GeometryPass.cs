using Jellyfish.Entities;
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
        var player = Player.Instance;
        if (player == null)
            return;

        base.Bind();

        SetMatrix4("view", player.GetViewMatrix());
        SetMatrix4("projection", player.GetProjectionMatrix());

        _diffuse.Bind(0);
        _normal?.Bind(1);
    }

    public override void Unbind()
    {
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, 0);

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.Texture2d, 0);

        base.Unbind();
    }

    public override void Unload()
    {
        _diffuse.Unload();
        _normal?.Unload();

        base.Unload();
    }
}