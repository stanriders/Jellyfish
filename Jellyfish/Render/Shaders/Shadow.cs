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

        SetBool("hasAlphaTest", BoundMaterial.GetParam<bool>("Cutout")); // todo: rename

        using var diffuse = BoundMaterial.GetParamTexture("Diffuse", true);
        BindTexture(0, diffuse);
    }
}