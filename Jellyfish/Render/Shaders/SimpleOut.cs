
namespace Jellyfish.Render.Shaders
{
    public class SimpleOut : Shader
    {
        private readonly Texture _rtColor;

        public SimpleOut(Texture color) :
            base("shaders/Screenspace.vert", null, "shaders/SimpleOut.frag")
        {
            _rtColor = color;
        }

        public override void Bind()
        {
            base.Bind();
            BindTexture(0, _rtColor);
        }

        public override void Unload()
        {
            _rtColor.Unload();
            base.Unload();
        }
    }
}
