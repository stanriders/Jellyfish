using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jellyfish.Render
{
    public class ShaderManager
    {
        private readonly Dictionary<string, int> _shaders = new();

        public int? GetShader(string? name, ShaderType type)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            if (_shaders.TryGetValue(name, out var handle))
            {
                return handle;
            }

            Log.Context(this).Debug("Compiling shader {Path}...", path);

            handle = GL.CreateShader(type);

            _shaders.Add(name, handle);

            return handle;
        }

        public void RemoveShader(string name)
        {
            if (_shaders.TryGetValue(name, out var handle))
            {
                GL.DeleteShader(handle);
                _shaders.Remove(name);
            }
        }

    }
}
