using System.Linq;
using Jellyfish.Input;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render
{
    public class Sky
    {
        private readonly float[] _skyboxVertices = {
            // positions          
            -1.0f,  1.0f, -1.0f,
            -1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f, -1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

            1.0f, -1.0f, -1.0f,
            1.0f, -1.0f,  1.0f,
            1.0f,  1.0f,  1.0f,
            1.0f,  1.0f,  1.0f,
            1.0f,  1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,

            -1.0f, -1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            1.0f,  1.0f,  1.0f,
            1.0f,  1.0f,  1.0f,
            1.0f, -1.0f,  1.0f,
            -1.0f, -1.0f,  1.0f,

            -1.0f,  1.0f, -1.0f,
            1.0f,  1.0f, -1.0f,
            1.0f,  1.0f,  1.0f,
            1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f,  1.0f,
            -1.0f,  1.0f, -1.0f,

            -1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
            1.0f, -1.0f, -1.0f,
            1.0f, -1.0f, -1.0f,
            -1.0f, -1.0f,  1.0f,
            1.0f, -1.0f,  1.0f
        };

        private readonly Skybox _shader;
        private readonly VertexBuffer _vbo;
        private readonly VertexArray _vao;

        public Sky()
        {
            _vbo = new VertexBuffer("Skybox", _skyboxVertices, 3 * sizeof(float));
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
            GL.DepthFunc(DepthFunction.Lequal);
            _shader.Bind();
            _vao.Bind();
            GL.DrawArrays(PrimitiveType.Triangles, 0, _skyboxVertices.Length);

            GL.BindVertexArray(0);
            GL.UseProgram(0);
            
            GL.DepthFunc(DepthFunction.Less);
        }

        public void Unload()
        {
            _shader.Unload();
            _vao.Unload();
            _vbo.Unload();
        }
    }
}
