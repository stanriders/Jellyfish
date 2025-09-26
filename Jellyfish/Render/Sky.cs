using Jellyfish.Debug;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render
{
    public class Sky
    {
        private readonly Skybox _shader;
        private readonly VertexBuffer _vbo;
        private readonly VertexArray _vao;

        public Sky()
        {
            _vbo = new VertexBuffer("Skybox", CommonShapes.CubeFloat, 3 * sizeof(float));
            _vao = new VertexArray(_vbo, null);
            _shader = new Skybox();

            var vertexLocation = _shader.GetAttribLocation("aPosition");
            if (vertexLocation != null)
            {
                GL.EnableVertexArrayAttrib(_vao.Handle, vertexLocation.Value);
                GL.VertexArrayAttribFormat(_vao.Handle, vertexLocation.Value, 3, VertexAttribType.Float, false, 0);
                GL.VertexArrayAttribBinding(_vao.Handle, vertexLocation.Value, 0);
            }
        }

        public void Draw()
        {
            if (Engine.LightManager.Sun == null)
                return;

            _shader.Bind();
            _vao.Bind();

            GL.DepthFunc(DepthFunction.Lequal);
            GL.DrawArrays(PrimitiveType.Triangles, 0, CommonShapes.Cube.Length);
            PerformanceMeasurment.Increment("DrawCalls");
            GL.DepthFunc(DepthFunction.Less);

            _shader.Unbind();
            _vao.Unbind();
        }

        public void Unload()
        {
            _shader.Unload();
            _vao.Unload();
            _vbo.Unload();
        }
    }
}
