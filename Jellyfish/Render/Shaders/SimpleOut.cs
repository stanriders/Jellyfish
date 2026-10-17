
namespace Jellyfish.Render.Shaders
{
    public class SimpleOut : Shader
    {
        private readonly TextureRef _rtColor;

        public SimpleOut(TextureRef color) :
            base("shaders/Screenspace.vert", null, "shaders/SimpleOut.frag")
        {
            _rtColor = color;
        }

        public override void Bind()
        {
            base.Bind();
            BindTexture(0, _rtColor.Texture);
        }
    }
}
