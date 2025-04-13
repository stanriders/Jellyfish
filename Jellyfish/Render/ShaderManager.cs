using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render
{
    public static class ShaderManager
    {
        private static Dictionary<string, int> Shaders { get; } = new();

        public static int? GetShader(string? path, ShaderType type)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (Shaders.TryGetValue(path, out var handle))
            {
                return handle;
            }

            handle = GL.CreateShader(type);
            if (handle != 0)
            {
                GL.ShaderSource(handle, LoadSource(path));
                CompileShader(handle);
                Shaders.Add(path, handle);
            }

            return handle;
        }

        public static void RemoveShader(string path)
        {
            if (Shaders.TryGetValue(path, out var handle))
            {
                GL.DeleteShader(handle);
                Shaders.Remove(path);
            }
        }

        private static void CompileShader(int shader)
        {
            GL.CompileShader(shader);

            GL.GetShaderi(shader, ShaderParameterName.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                GL.GetShaderInfoLog(shader, out var error);
                throw new Exception($"Cant compile shader, {error}");
            }
        }

        private static string LoadSource(string path)
        {
            try
            {
                var builder = new StringBuilder();
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(stream, Encoding.UTF8);
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    if (line == null)
                        break;

                    if (line.StartsWith("#include"))
                    {
                        var includedFile = File.ReadAllText(Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, line.Replace("#include", "").Trim()));
                        builder.AppendLine(includedFile);
                        continue;
                    }
                    builder.AppendLine(line);
                }
                return builder.ToString();
            }
            catch (Exception ex)
            {
                Log.Context("ShaderManager").Error(ex, "Failed to load shader {Path}", path);
                return string.Empty;
            }
        }
    }
}
