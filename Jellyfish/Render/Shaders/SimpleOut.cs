
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Shaders
{
    class SimpleOut : Shader
    {
        private readonly Texture texture;

        public SimpleOut(string diffusePath) : base("shaders/SimpleOut.vert", "shaders/SimpleOut.frag")
        {
            texture = new Texture(diffusePath);
            texture.Draw();

            // move to vertex buffer?
            var vertexLocation = GetAttribLocation("aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            var texCoordLocation = GetAttribLocation("aTexCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            var normalLocation = GetAttribLocation("aNormal");
            GL.EnableVertexAttribArray(normalLocation);
            GL.VertexAttribPointer(normalLocation, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 5 * sizeof(float));
        }

        public override void Draw()
        {
            texture.Draw(TextureUnit.Texture0); // diffuse
            base.Draw();
        }
    }
}
