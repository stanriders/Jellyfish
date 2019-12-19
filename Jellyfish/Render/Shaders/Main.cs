
using Jellyfish.Render.Lighting;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Shaders
{
    class Main : Shader
    {
        private readonly Texture diffuse;
        private readonly Texture normal;

        public Main(string diffusePath, string normalPath = null) : base("shaders/Main.vert", null, "shaders/Main.frag")
        {
            diffuse = new Texture(diffusePath);
            diffuse.Draw();

            if (!string.IsNullOrEmpty(normalPath))
            {
                normal = new Texture(normalPath);
                normal.Draw();
            }

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
            SetVector3("cameraPos", Camera.Position);
            SetMatrix4("view", Camera.GetViewMatrix());
            SetMatrix4("projection", Camera.GetProjectionMatrix());

            var lights = LightManager.GetLightSources();
            SetInt("lightSourcesCount", lights.Length);
            for (int i = 0; i < lights.Length; i++)
            {
                SetVector3($"lightSources[{i}].position", lights[i].Position);
                SetVector3($"lightSources[{i}].diffuse", new Vector3(lights[i].Color.R * lights[i].Color.A, lights[i].Color.G * lights[i].Color.A, lights[i].Color.B * lights[i].Color.A));
                SetVector3($"lightSources[{i}].ambient", new Vector3(0.1f, 0.1f, 0.1f));
                if (lights[i] is PointLight point)
                {
                    SetFloat($"lightSources[{i}].constant", point.Constant);
                    SetFloat($"lightSources[{i}].linear", point.Linear);
                    SetFloat($"lightSources[{i}].quadratic", point.Quadratic);
                }
            }

            diffuse.Draw(TextureUnit.Texture0);
            normal?.Draw(TextureUnit.Texture1);
            base.Draw();
        }
    }
}
