
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers
{
    public class VertexArray
    {
        private readonly int vaoHandler;

        public VertexArray()
        {
            vaoHandler = GL.GenVertexArray();
            GL.BindVertexArray(vaoHandler);
        }

        public void Bind()
        {
            GL.BindVertexArray(vaoHandler);
        }
        public void Unload()
        {
            GL.BindVertexArray(0);
            GL.DeleteVertexArray(vaoHandler);
        }
    }
}
