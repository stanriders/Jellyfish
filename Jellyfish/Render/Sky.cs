using System.Linq;
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
            _vao = new VertexArray();

            _vbo = new VertexBuffer(_skyboxVertices.Length * sizeof(float));
            GL.BufferData(BufferTarget.ArrayBuffer, _skyboxVertices.Length * sizeof(float), _skyboxVertices.ToArray(), BufferUsageHint.StaticDraw);

            _vbo.Bind();

            _shader = new Skybox();

            GL.BindVertexArray(0);
            GL.UseProgram(0);
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
    }
}
