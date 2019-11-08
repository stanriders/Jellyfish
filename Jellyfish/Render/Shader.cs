using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render
{
    public abstract class Shader
    {
        private readonly int shaderHandle;

        private readonly Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

        protected Shader(string vertPath, string geomPath, string fragPath, string tessControlPath = null, string tessEvalPath = null, string compPath = null)
        {
            // compile shaders
            int vertexShader = 0;
            if (!string.IsNullOrEmpty(vertPath))
            {
                vertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertexShader, LoadSource(vertPath));
                CompileShader(vertexShader);
            }

            int geometryShader = 0;
            if (!string.IsNullOrEmpty(geomPath))
            {
                geometryShader = GL.CreateShader(ShaderType.GeometryShader);
                GL.ShaderSource(geometryShader, LoadSource(geomPath));
                CompileShader(geometryShader);
            }

            int fragmentShader = 0;
            if (!string.IsNullOrEmpty(fragPath))
            {
                fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragmentShader, LoadSource(fragPath));
                CompileShader(fragmentShader);
            }

            int tesselationControlShader = 0;
            if (!string.IsNullOrEmpty(tessControlPath))
            {
                tesselationControlShader = GL.CreateShader(ShaderType.TessControlShader);
                GL.ShaderSource(tesselationControlShader, LoadSource(tessControlPath));
                CompileShader(tesselationControlShader);
            }

            int tesselationEvaluationShader = 0;
            if (!string.IsNullOrEmpty(tessEvalPath))
            {
                tesselationEvaluationShader = GL.CreateShader(ShaderType.TessEvaluationShader);
                GL.ShaderSource(tesselationEvaluationShader, LoadSource(tessEvalPath));
                CompileShader(tesselationEvaluationShader);
            }

            int computeShader = 0;
            if (!string.IsNullOrEmpty(compPath))
            {
                computeShader = GL.CreateShader(ShaderType.ComputeShader);
                GL.ShaderSource(computeShader, LoadSource(compPath));
                CompileShader(computeShader);
            }

            // create shader program
            shaderHandle = GL.CreateProgram();

            if (!string.IsNullOrEmpty(vertPath))
                GL.AttachShader(shaderHandle, vertexShader);

            if (!string.IsNullOrEmpty(geomPath))
                GL.AttachShader(shaderHandle, geometryShader);

            if (!string.IsNullOrEmpty(fragPath))
                GL.AttachShader(shaderHandle, fragmentShader);

            if (!string.IsNullOrEmpty(tessControlPath))
                GL.AttachShader(shaderHandle, tesselationControlShader);

            if (!string.IsNullOrEmpty(tessEvalPath))
                GL.AttachShader(shaderHandle, tesselationEvaluationShader);

            if (!string.IsNullOrEmpty(compPath))
                GL.AttachShader(shaderHandle, computeShader);

            LinkProgram(shaderHandle);

            // remove singular shaders
            if (!string.IsNullOrEmpty(vertPath))
            {
                GL.DetachShader(shaderHandle, vertexShader);
                GL.DeleteShader(vertexShader);
            }

            if (!string.IsNullOrEmpty(geomPath))
            {
                GL.DetachShader(shaderHandle, geometryShader);
                GL.DeleteShader(geometryShader);
            }

            if (!string.IsNullOrEmpty(fragPath))
            {
                GL.DetachShader(shaderHandle, fragmentShader);
                GL.DeleteShader(fragmentShader);
            }

            if (!string.IsNullOrEmpty(tessControlPath))
            {
                GL.DetachShader(shaderHandle, tesselationControlShader);
                GL.DeleteShader(tesselationControlShader);
            }

            if (!string.IsNullOrEmpty(tessEvalPath))
            {
                GL.DetachShader(shaderHandle, tesselationEvaluationShader);
                GL.DeleteShader(tesselationEvaluationShader);
            }

            if (!string.IsNullOrEmpty(compPath))
            {
                GL.DetachShader(shaderHandle, computeShader);
                GL.DeleteShader(computeShader);
            }

            GL.GetProgram(shaderHandle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);
            for (var i = 0; i < numberOfUniforms; i++)
            {
                var key = GL.GetActiveUniform(shaderHandle, i, out _, out _);
                var location = GL.GetUniformLocation(shaderHandle, key);

                uniformLocations.Add(key, location);
            }
        }

        public void Unload()
        {
            if (shaderHandle != 0)
            {
                GL.UseProgram(0);
                GL.DeleteProgram(shaderHandle);
            }
        }

        private static void CompileShader(int shader)
        {
            GL.CompileShader(shader);

            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                throw new Exception($"Cant compile shader, {GL.GetShaderInfoLog(shader)}");
            }
        }

        private static void LinkProgram(int program)
        {
            GL.LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if (code != (int)All.True)
            {
                throw new Exception($"Cant link shader, {GL.GetProgramInfoLog(program)}");
            }
        }

        public virtual void Draw()
        {
            if (shaderHandle != 0)
            {
                GL.UseProgram(shaderHandle);
            }
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(shaderHandle, attribName);
        }

        private static string LoadSource(string path)
        {
            using (var sr = new StreamReader(path, Encoding.UTF8))
            {
                return sr.ReadToEnd();
            }
        }

        /// <summary>
        /// Set a uniform int on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetInt(string name, int data)
        {
            GL.UseProgram(shaderHandle);
            GL.Uniform1(uniformLocations[name], data);
        }

        /// <summary>
        /// Set a uniform float on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetFloat(string name, float data)
        {
            GL.UseProgram(shaderHandle);
            GL.Uniform1(uniformLocations[name], data);
        }

        /// <summary>
        /// Set a uniform Matrix4 on this shader
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        /// <remarks>
        ///   <para>
        ///   The matrix is transposed before being sent to the shader.
        ///   </para>
        /// </remarks>
        public void SetMatrix4(string name, Matrix4 data)
        {
            GL.UseProgram(shaderHandle);
            GL.UniformMatrix4(uniformLocations[name], true, ref data);
        }

        /// <summary>
        /// Set a uniform Vector3 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetVector2(string name, Vector2 data)
        {
            GL.UseProgram(shaderHandle);
            GL.Uniform2(uniformLocations[name], data);
        }

        /// <summary>
        /// Set a uniform Vector3 on this shader.
        /// </summary>
        /// <param name="name">The name of the uniform</param>
        /// <param name="data">The data to set</param>
        public void SetVector3(string name, Vector3 data)
        {
            GL.UseProgram(shaderHandle);
            GL.Uniform3(uniformLocations[name], data);
        }
    }
}
