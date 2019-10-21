using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers
{
    public class IndexBuffer
    {
        private readonly int handler;

        public IndexBuffer(uint[] indices)
        {
            handler = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, handler);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
        }

        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, handler);
        }

        public void Unload()
        {
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.DeleteBuffer(handler);
        }
    }
}
