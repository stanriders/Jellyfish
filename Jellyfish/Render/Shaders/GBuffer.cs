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
            var diffuse = BoundMaterial.GetParamTexture("Diffuse", true);
            var normal = BoundMaterial.GetParamTexture("Normal");
            var metRough = BoundMaterial.GetParamTexture("MetalRoughness");

            SetBool("hasAlphaTest", BoundMaterial.GetParam<bool>("Cutout")); // todo: rename
            SetBool("hasNormalMap", normal != null);
            SetBool("hasMetRough", metRough != null);

            BindTexture(0, diffuse);
            BindTexture(1, normal);
            BindTexture(2, metRough);
        }
        else
        {
            SetBool("hasNormalMap", false);
            SetBool("hasAlphaTest", false);
            SetBool("hasMetRough", false);
        }
    }
}