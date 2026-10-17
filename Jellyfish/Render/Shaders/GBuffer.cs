namespace Jellyfish.Render.Shaders;

public class GBuffer() : Shader("shaders/Main.vert", null, "shaders/GBuffer.frag")
{
    public override void Bind()
    {
        base.Bind();

        SetMatrix4("view", Engine.MainViewport.GetViewMatrix());
        SetMatrix4("projection", Engine.MainViewport.GetProjectionMatrix());

        if (BoundMaterial != null)
        {
            Texture? diffuse = null;
            Texture? normal = null;

            if (BoundMaterial.TryGetParam<string>("Normal", out var normalPath))
            {
                normal = Engine.TextureManager.GetTexture(new TextureParams
                    { Path = $"{BoundMaterial.Directory}/{normalPath}" }).Texture;
            }

            if (BoundMaterial.TryGetParam<string>("Diffuse", out var diffusePath))
            {
                diffuse = Engine.TextureManager.GetTexture(new TextureParams
                    { Path = $"{BoundMaterial.Directory}/{diffusePath}", Srgb = true }).Texture;
            }

            var hasDiffuse = diffuse != null;
            var hasNormal = normal != null;

            SetBool("hasAlphaTest", BoundMaterial.GetParam<bool>("Cutout")); // todo: rename

            if (hasDiffuse)
            {
                BindTexture(0, diffuse);
            }

            SetBool("hasNormalMap", hasNormal);
            if (hasNormal)
            {
                BindTexture(1, normal);
            }

            diffuse?.Unload();
            normal?.Unload();
        }
        else
        {
            SetBool("hasNormalMap", false);
            SetBool("hasAlphaTest", false);
        }
    }
}