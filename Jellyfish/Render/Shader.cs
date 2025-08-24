using System;
using System.Collections.Generic;
using System.IO;
using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render;

public class Uniform
{
    public required int Location { get; set; }
    public required string Name { get; set; }
    public object? Value { get; set; }
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
    private readonly string? _compPath;

    private readonly List<FileSystemWatcher> _watchers = new();
    private bool _reloading;

    private bool _complainedAboutMissingUniforms;

    protected Shader(string vertPath, string? geomPath, string fragPath, string? tessControlPath = null,
        string? tessEvalPath = null, string? compPath = null)
    {
        _vertPath = vertPath;
        _geomPath = geomPath;
        _fragPath = fragPath;
        _tessControlPath = tessControlPath;
        _tessEvalPath = tessEvalPath;
        _compPath = compPath;

        _shaderHandle = LoadShader();

        var vertWatcher = new FileSystemWatcher(Path.GetDirectoryName(vertPath)!, Path.GetFileName(vertPath))
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        vertWatcher.Changed += OnChanged;
        _watchers.Add(vertWatcher);

        if (!string.IsNullOrEmpty(geomPath))
        {
            var geomWatcher = new FileSystemWatcher(Path.GetDirectoryName(geomPath)!, Path.GetFileName(geomPath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            geomWatcher.Changed += OnChanged;
            _watchers.Add(geomWatcher);
        }

        var fragWatcher = new FileSystemWatcher(Path.GetDirectoryName(fragPath)!, Path.GetFileName(fragPath))
        {
            NotifyFilter = NotifyFilters.LastWrite,
            EnableRaisingEvents = true
        };
        fragWatcher.Changed += OnChanged;
        _watchers.Add(fragWatcher);

        if (!string.IsNullOrEmpty(tessControlPath))
        {
            var tessControlWatcher = new FileSystemWatcher(Path.GetDirectoryName(tessControlPath)!, Path.GetFileName(tessControlPath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            tessControlWatcher.Changed += OnChanged;
            _watchers.Add(tessControlWatcher);
        }

        if (!string.IsNullOrEmpty(tessEvalPath))
        {
            var tessEvalWatcher = new FileSystemWatcher(Path.GetDirectoryName(tessEvalPath)!, Path.GetFileName(tessEvalPath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            tessEvalWatcher.Changed += OnChanged;
            _watchers.Add(tessEvalWatcher);
        }

        if (!string.IsNullOrEmpty(compPath))
        {
            var compWatcher = new FileSystemWatcher(Path.GetDirectoryName(compPath)!, Path.GetFileName(compPath))
            {
                NotifyFilter = NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            compWatcher.Changed += OnChanged;
            _watchers.Add(compWatcher);
        }
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

        GL.ObjectLabel(ObjectIdentifier.Program, (uint)handle, GetType().Name.Length, GetType().Name);

        // compile shaders
        var vertexShader = Engine.ShaderManager.GetShader(_vertPath, ShaderType.VertexShader);
        var geometryShader = Engine.ShaderManager.GetShader(_geomPath, ShaderType.GeometryShader);
        var fragmentShader = Engine.ShaderManager.GetShader(_fragPath, ShaderType.FragmentShader);
        var tesselationControlShader = Engine.ShaderManager.GetShader(_tessControlPath, ShaderType.TessControlShader);
        var tesselationEvaluationShader = Engine.ShaderManager.GetShader(_tessEvalPath, ShaderType.TessEvaluationShader);
        var computeShader = Engine.ShaderManager.GetShader(_compPath, ShaderType.ComputeShader);

        if (vertexShader != null)
            GL.AttachShader(handle, vertexShader.Value);

        if (geometryShader != null)
            GL.AttachShader(handle, geometryShader.Value);

        if (fragmentShader != null)
            GL.AttachShader(handle, fragmentShader.Value);

        if (tesselationControlShader != null)
            GL.AttachShader(handle, tesselationControlShader.Value);

        if (tesselationEvaluationShader != null)
            GL.AttachShader(handle, tesselationEvaluationShader.Value);

        if (computeShader != null)
            GL.AttachShader(handle, computeShader.Value);

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

        if (computeShader != null)
        {
            GL.DetachShader(handle, computeShader.Value);
        }

        GL.GetProgrami(handle, ProgramProperty.ActiveUniforms, out var numberOfUniforms);
        for (uint i = 0; i < numberOfUniforms; i++)
        {
            GL.GetActiveUniform(handle, i, 128, out _, out var size, out var uniformType, out var key);
            var location = GL.GetUniformLocation(handle, key);

            // what if it's not an array somehow?..
            var nameWithoutArray = key[..^3];

            if (size > 1)
            {
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

        if (uniform.Value != null)
        {
            if (((T)uniform.Value).Equals(data))
                return null;
        }

        uniform.Value = data;

        if (bind)
            Bind();

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
            GL.Uniform1i(uniform.Location, data ? 1 : 0);
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
            GL.Uniform1i(uniform.Location, data);
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
            GL.Uniform1f(uniform.Location, data);
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
            GL.UniformMatrix4f(uniform.Location, 1, transpose, ref data);
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
            GL.Uniform2f(uniform.Location, data.X, data.Y);
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
            GL.Uniform3f(uniform.Location, data.X, data.Y, data.Z);
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
            GL.Uniform3f(uniform.Location, data[0], data[1], data[2]);
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
            GL.Uniform4f(uniform.Location, data.X, data.Y, data.Z, data.W);
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
            GL.Uniform4f(uniform.Location, data[0], data[1], data[2], data[3]);
    }
}