namespace Jellyfish.Render.Shaders;

public class Shadow() : Shader("shaders/Shadow.vert", null, "shaders/Shadow.frag")
{
    public override void Bind()
    {
        base.Bind();

        if (BoundMaterial == null)
        {
            SetBool("hasAlphaTest", false);
            return;
        }

        Texture? diffuse = null;

        if (BoundMaterial.TryGetParam<string>("Diffuse", out var diffusePath))
        {
            diffuse = Engine.TextureManager.GetTexture(new TextureParams
                { Path = $"{BoundMaterial.Directory}/{diffusePath}", Srgb = true }).Texture;
        }

        var hasDiffuse = diffuse != null;

        SetBool("hasAlphaTest", BoundMaterial.GetParam<bool>("Cutout")); // todo: rename

        if (hasDiffuse)
        {
            BindTexture(0, diffuse);
        }

        diffuse?.Unload(); // todo: textureref class
    }
}