using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jellyfish.Render;

public struct Uniform
{
    public required int Location { get; set; }
    public required string Name { get; set; }
    public int? ValueHash { get; set; }
}

public abstract class Shader
{
    private int _shaderHandle;

    private readonly Dictionary<string, Uniform> _uniforms = new();

    private readonly string _vertPath;
    private readonly string? _geomPath;
    private readonly string _fragPath;
    private readonly string? _tessControlPath;
    private readonly string? _tessEvalPath;

    private readonly List<FileSystemWatcher> _watchers = new();
    private readonly List<uint> _boundTextures = new();
    private bool _reloading;

    private bool _complainedAboutMissingUniforms;

    protected Shader(string vertPath, string? geomPath, string fragPath, string? tessControlPath = null, string? tessEvalPath = null)
    {
        _vertPath = vertPath;
        _geomPath = geomPath;
        _fragPath = fragPath;
        _tessControlPath = tessControlPath;
        _tessEvalPath = tessEvalPath;

        _shaderHandle = LoadShader();
    }

    private void AddWatcher(string? path)
    {
        if (string.IsNullOrEmpty(path)) 
            return;

        var watcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Path.GetFileName(path))
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        watcher.Changed += OnChanged;
        _watchers.Add(watcher);
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        if (_reloading)
            return;

        _reloading = true;

        Log.Context(this).Information("Reloading shader {File}...", e.Name);
        RenderScheduler.Schedule(() =>
        {
            try
            {
                Engine.ShaderManager.RemoveShader(e.FullPath.Replace(@"\", "/"));

                var newHandle = LoadShader();
                var oldHandle = _shaderHandle;

                _shaderHandle = newHandle;

                if (oldHandle != 0)
                {
                    GL.UseProgram(0);
                    GL.DeleteProgram(oldHandle);
                }
            }
            catch (Exception exception)
            {
                Log.Context(this).Error(exception.ToString());
            }
            finally
            {
                _reloading = false;
            }
        });
    }

    public virtual void Unload()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        if (_shaderHandle != 0)
        {
            GL.UseProgram(0);
            GL.DeleteProgram(_shaderHandle);
        }
    }

    private int LoadShader()
    {
        // cleanup
        _uniforms.Clear();

        // create shader program
        var handle = GL.CreateProgram();
        if (handle == 0)
            return 0;

        GL.ObjectLabel(ObjectIdentifier.Program, handle, GetType().Name.Length, GetType().Name);

        // compile shaders
        var vertexShader = Engine.ShaderManager.GetShader(_vertPath, ShaderType.VertexShader);
        var geometryShader = Engine.ShaderManager.GetShader(_geomPath, ShaderType.GeometryShader);
        var fragmentShader = Engine.ShaderManager.GetShader(_fragPath, ShaderType.FragmentShader);
        var tesselationControlShader = Engine.ShaderManager.GetShader(_tessControlPath, ShaderType.TessControlShader);
        var tesselationEvaluationShader = Engine.ShaderManager.GetShader(_tessEvalPath, ShaderType.TessEvaluationShader);

        if (vertexShader != null)
        {
            CompileShader(_vertPath, vertexShader.Value);
            GL.AttachShader(handle, vertexShader.Value);
            AddWatcher(_vertPath);
        }

        if (geometryShader != null)
        {
            CompileShader(_geomPath, geometryShader.Value);
            GL.AttachShader(handle, geometryShader.Value);
            AddWatcher(_geomPath);
        }

        if (fragmentShader != null)
        {
            CompileShader(_fragPath, fragmentShader.Value);
            GL.AttachShader(handle, fragmentShader.Value);
            AddWatcher(_fragPath);
        }

        if (tesselationControlShader != null)
        {
            CompileShader(_tessControlPath, tesselationControlShader.Value);
            GL.AttachShader(handle, tesselationControlShader.Value);
            AddWatcher(_tessControlPath);
        }

        if (tesselationEvaluationShader != null)
        {
            CompileShader(_tessEvalPath, tesselationEvaluationShader.Value);
            GL.AttachShader(handle, tesselationEvaluationShader.Value);
            AddWatcher(_tessEvalPath);
        }

        LinkProgram(handle);

        // remove singular shaders
        if (vertexShader != null)
        {
            GL.DetachShader(handle, vertexShader.Value);
        }

        if (geometryShader != null)
        {
            GL.DetachShader(handle, geometryShader.Value);
        }

        if (fragmentShader != null)
        {
            GL.DetachShader(handle, fragmentShader.Value);
        }

        if (tesselationControlShader != null)
        {
            GL.DetachShader(handle, tesselationControlShader.Value);
        }

        if (tesselationEvaluationShader != null)
        {
            GL.DetachShader(handle, tesselationEvaluationShader.Value);
        }

        GL.GetProgrami(handle, ProgramProperty.ActiveUniforms, out var numberOfUniforms);
        for (uint i = 0; i < numberOfUniforms; i++)
        {
            GL.GetActiveUniform(handle, i, 128, out _, out var size, out var uniformType, out var key);
            var location = GL.GetUniformLocation(handle, key);

            if (size > 1)
            {
                // what if it's not an array somehow?..
                var nameWithoutArray = key[..^3];

                for (var j = 0; j < size; j++)
                {
                    _uniforms.Add($"{nameWithoutArray}[{j}]", new Uniform { Location = location + j, Name = $"{nameWithoutArray}[{j}]" });
                }
            }
            else
            {
                _uniforms.Add(key, new Uniform { Location = location, Name = key });
            }
        }

        _complainedAboutMissingUniforms = false;

        return handle;
    }

    private static void LinkProgram(int program)
    {
        GL.LinkProgram(program);

        GL.GetProgrami(program, ProgramProperty.LinkStatus, out var code);
        if (code != (int)All.True)
        {
            GL.GetShaderInfoLog(program, out var error);
            throw new Exception($"Cant link shader, {error}");
        }
    }

    public virtual void Bind()
    {
        if (_shaderHandle != 0)
        {
            GL.UseProgram(_shaderHandle);
        }
        else
        {
            Log.Context(this).Error("Tried drawing shader with no handle!");
        }
    }

    public virtual void Unbind()
    {
        foreach (var bindTexture in _boundTextures)
        {
            GL.BindTextureUnit(bindTexture, 0);
        }
        _boundTextures.Clear();

        GL.UseProgram(0);
    }

    public uint? GetAttribLocation(string attribName)
    {
        var attrib = GL.GetAttribLocation(_shaderHandle, attribName);
        if (attrib == -1)
            return null;

        return (uint)attrib;
    }

    private Uniform? SetUniform<T>(string name, T data, bool bind = false)
    {
        if (!_uniforms.TryGetValue(name, out var uniform))
        {
            if (!_complainedAboutMissingUniforms)
            {
                Log.Context(this).Error("Uniform {Name} isn't found!", name);
                _complainedAboutMissingUniforms = true;
            }

            return null;
        }

        var dataHash = data?.GetHashCode();
        if (uniform.ValueHash != null)
        {
            if (uniform.ValueHash == dataHash)
                return null;
        }

        uniform.ValueHash = dataHash;

        if (bind)
            Bind();

        _uniforms[name] = uniform;

        return uniform;
    }

    /// <summary>
    ///     Set a uniform int on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetBool(string name, bool data, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.Uniform1i(uniform.Value.Location, data ? 1 : 0);
    }

    /// <summary>
    ///     Set a uniform int on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetInt(string name, int data, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.Uniform1i(uniform.Value.Location, data);
    }

    /// <summary>
    ///     Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetFloat(string name, float data, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.Uniform1f(uniform.Value.Location, data);
    }

    /// <summary>
    ///     Set a uniform Matrix4 on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="transpose"></param>
    /// <param name="bind"></param>
    public void SetMatrix4(string name, Matrix4 data, bool transpose = false, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.UniformMatrix4f(uniform.Value.Location, 1, transpose, ref data);
    }

    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector2(string name, Vector2 data, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.Uniform2f(uniform.Value.Location, data.X, data.Y);
    }

    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector3(string name, Vector3 data, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.Uniform3f(uniform.Value.Location, data.X, data.Y, data.Z);
    }

    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector3(string name, float[] data, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.Uniform3f(uniform.Value.Location, data[0], data[1], data[2]);
    }

    public void SetVector3(string name, double[] data, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.Uniform3d(uniform.Value.Location, data[0], data[1], data[2]);
    }
    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector4(string name, Vector4 data, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.Uniform4f(uniform.Value.Location, data.X, data.Y, data.Z, data.W);
    }

    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector4(string name, float[] data, bool bind = false)
    {
        var uniform = SetUniform(name, data, bind);
        if (uniform != null)
            GL.Uniform4f(uniform.Value.Location, data[0], data[1], data[2], data[3]);
    }

    public void BindTexture(uint sampler, Texture? texture)
    {
        if (texture == null)
            return;

        if (_boundTextures.Contains(sampler))
            throw new Exception("Trying to bind to a sampler that wasn't unbound");

        texture.Bind(sampler);
        _boundTextures.Add(sampler);
    }

    private void CompileShader(string? path, int shader)
    {
        if (string.IsNullOrEmpty(path))
            return;

        var shaderSource = LoadSource(path);
        GL.ShaderSource(shader, shaderSource);

        GL.CompileShader(shader);

        GL.GetShaderi(shader, ShaderParameterName.CompileStatus, out var code);
        if (code != (int)All.True)
        {
            GL.GetShaderInfoLog(shader, out var error);
            Log.Context(this).Error("Failed to compile shader {Path}: {Error}", path, error);
            throw new Exception($"Failed to compile shader {path}:\n{error}");
        }
    }

    private string LoadSource(string path)
    {
        try
        {
            var builder = new StringBuilder();
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var sr = new StreamReader(stream, Encoding.UTF8);
            while (!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                if (line == null)
                    break;

                if (line.StartsWith("#include"))
                {
                    var includePath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, line.Replace("#include", "").Trim());

                    var includedFile = LoadSource(includePath);
                    builder.AppendLine(includedFile);
                    continue;
                }
                builder.AppendLine(line);
            }
            return builder.ToString();
        }
        catch (Exception ex)
        {
            Log.Context(this).Error(ex, "Failed to load shader {Path}", path);
            throw;
        }
    }
}