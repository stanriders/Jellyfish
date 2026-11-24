
using System.Collections.Generic;
using Jellyfish.Render.Shaders;
using Jellyfish.Render.Shaders.IBL;

namespace Jellyfish.Render
{
    public class ShaderManager
    {
        // todo: automatic registration
        private readonly Dictionary<string, Shader> _shaders = new()
        {
            {"Imgui", new Imgui()},
            {"Main", new Main()},
            {"Simple", new Main()},
            {"Shadow", new Shadow()},
            {"GBuffer", new Shaders.GBuffer()},
            {"Prefiltering", new Prefiltering()}
        };

        public Shader? GetShader(string? name)
        {
            if (name == null)
                return null;

            if (_shaders.TryGetValue(name, out var existingShader))
            {
                return existingShader;
            }

            throw new KeyNotFoundException($"Can't find shader {name}");
        }

        public void Unload()
        {
            foreach (var shader in _shaders)
            {
                shader.Value.Unload();
            }
        }
    }
}
