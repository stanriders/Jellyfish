using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render
{
    public class ShaderManager
    {
        private readonly Dictionary<string, int> _shaders = new();

        public int? GetShader(string? path, ShaderType type)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (_shaders.TryGetValue(path, out var handle))
            {
                return handle;
            }

            handle = GL.CreateShader(type);
            if (handle != 0)
            {
                var shaderSource = LoadSource(path);
                GL.ShaderSource(handle, shaderSource);
                CompileShader(handle);
                _shaders.Add(path, handle);
            }

            return handle;
        }

        public void RemoveShader(string path)
        {
            if (_shaders.TryGetValue(path, out var handle))
            {
                GL.DeleteShader(handle);
                _shaders.Remove(path);
            }
        }

        private void CompileShader(int shader)
        {
            GL.CompileShader(shader);

            GL.GetShaderi(shader, ShaderParameterName.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                GL.GetShaderInfoLog(shader, out var error);
                throw new Exception($"Cant compile shader:\n{error}");
            }
        }

        private string LoadSource(string path)
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
                        var includePath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, line.Replace("#include", "").Trim());

                        var includedFile = LoadDependency(includePath);
                        var fileLines = includedFile.Split('\n');
                        foreach (var fileLine in fileLines)
                        {
                            if (fileLine.StartsWith("#include"))
                            {
                                var subIncludePath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, fileLine.Replace("#include", "").Trim());
                                builder.AppendLine(LoadDependency(subIncludePath));
                                continue;
                            }
                            builder.AppendLine(fileLine);
                        }
                        continue;
                    }
                    builder.AppendLine(line);
                }
                return builder.ToString();
            }
            catch (Exception ex)
            {
                Log.Context(this).Error(ex, "Failed to load shader {Path}", path);
                return string.Empty;
            }
        }

        private string LoadDependency(string includePath)
        {
            if (!File.Exists(includePath))
                throw new FileNotFoundException();

            var includedFile = File.ReadAllText(includePath);

            return includedFile;
        }
    }
}
