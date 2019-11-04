
using Jellyfish.Render.Buffers;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render
{
    public class Mesh
    {
        protected VertexBuffer vbo;
        protected IndexBuffer ibo;
        protected VertexArray vao;

        protected Shader shader;
        protected MeshInfo mesh = new MeshInfo();

        public Vector3 Position = Vector3.Zero;
        public Vector3 Rotation = Vector3.Zero;

        public virtual PrimitiveType PrimitiveType { get; set; } = PrimitiveType.Triangles;

        public Mesh()
        {
        }

        public Mesh(MeshInfo mesh)
        {
            this.mesh = mesh;
            CreateBuffers();
        }

        public void AddShader(Shader shader)
        {
            this.shader = shader;
            shader.Draw();
        }

        public void CreateBuffers()
        {
            vbo = new VertexBuffer(mesh.Vertices.ToArray(), mesh.UVs.ToArray(), mesh.Normals.ToArray());

            if (mesh.Indices != null && mesh.Indices.Count > 0)
                ibo = new IndexBuffer(mesh.Indices.ToArray());

            vao = new VertexArray();
            vbo.Bind();
            ibo?.Bind();
        }

        public void Draw()
        {
            shader.Draw();
            vao.Bind();

            var transform = Matrix4.Identity * Matrix4.CreateTranslation(Position);

            shader.SetMatrix4("transform", transform);

            var rotation = Matrix4.Identity *
                            Matrix4.CreateRotationX(Rotation.X) *
                            Matrix4.CreateRotationY(Rotation.Y) *
                            Matrix4.CreateRotationZ(Rotation.Z);

            shader.SetMatrix4("rotation", rotation);

            // fixme: move to shader or some kind of shared thing
            shader.SetVector3("cameraPos", Camera.Position);
            shader.SetMatrix4("view", Camera.GetViewMatrix());
            shader.SetMatrix4("projection", Camera.GetProjectionMatrix());

            if (ibo != null)
                GL.DrawElements(PrimitiveType, mesh.Indices.Count, DrawElementsType.UnsignedInt, 0);
            else
                GL.DrawArrays(PrimitiveType, 0, vbo.Length);
        }

        public void Unload()
        {
            vbo.Unload();
            ibo?.Unload();
            vao.Unload();
            shader.Unload();
        }
    }
}
