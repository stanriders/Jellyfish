using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Serilog;

namespace Jellyfish.Render;

public abstract class Shader
{
    private readonly int _shaderHandle;

    private readonly Dictionary<string, int> _uniformLocations = new();

    protected Shader(string vertPath, string? geomPath, string fragPath, string? tessControlPath = null,
        string? tessEvalPath = null, string? compPath = null)
    {
        // create shader program
        _shaderHandle = GL.CreateProgram();

        // compile shaders
        var vertexShader = 0;
        if (!string.IsNullOrEmpty(vertPath))
        {
            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, LoadSource(vertPath));
            CompileShader(vertexShader);
        }

        var geometryShader = 0;
        if (!string.IsNullOrEmpty(geomPath))
        {
            geometryShader = GL.CreateShader(ShaderType.GeometryShader);
            GL.ShaderSource(geometryShader, LoadSource(geomPath));
            CompileShader(geometryShader);
        }

        var fragmentShader = 0;
        if (!string.IsNullOrEmpty(fragPath))
        {
            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, LoadSource(fragPath));
            CompileShader(fragmentShader);
        }

        var tesselationControlShader = 0;
        if (!string.IsNullOrEmpty(tessControlPath))
        {
            tesselationControlShader = GL.CreateShader(ShaderType.TessControlShader);
            GL.ShaderSource(tesselationControlShader, LoadSource(tessControlPath));
            CompileShader(tesselationControlShader);
        }

        var tesselationEvaluationShader = 0;
        if (!string.IsNullOrEmpty(tessEvalPath))
        {
            tesselationEvaluationShader = GL.CreateShader(ShaderType.TessEvaluationShader);
            GL.ShaderSource(tesselationEvaluationShader, LoadSource(tessEvalPath));
            CompileShader(tesselationEvaluationShader);
        }

        var computeShader = 0;
        if (!string.IsNullOrEmpty(compPath))
        {
            computeShader = GL.CreateShader(ShaderType.ComputeShader);
            GL.ShaderSource(computeShader, LoadSource(compPath));
            CompileShader(computeShader);
        }

        if (vertexShader != 0)
            GL.AttachShader(_shaderHandle, vertexShader);

        if (geometryShader != 0)
            GL.AttachShader(_shaderHandle, geometryShader);

        if (fragmentShader != 0)
            GL.AttachShader(_shaderHandle, fragmentShader);

        if (tesselationControlShader != 0)
            GL.AttachShader(_shaderHandle, tesselationControlShader);

        if (tesselationEvaluationShader != 0)
            GL.AttachShader(_shaderHandle, tesselationEvaluationShader);

        if (computeShader != 0)
            GL.AttachShader(_shaderHandle, computeShader);

        LinkProgram(_shaderHandle);

        // remove singular shaders
        if (vertexShader != 0)
        {
            GL.DetachShader(_shaderHandle, vertexShader);
            GL.DeleteShader(vertexShader);
        }

        if (geometryShader != 0)
        {
            GL.DetachShader(_shaderHandle, geometryShader);
            GL.DeleteShader(geometryShader);
        }

        if (fragmentShader != 0)
        {
            GL.DetachShader(_shaderHandle, fragmentShader);
            GL.DeleteShader(fragmentShader);
        }

        if (tesselationControlShader != 0)
        {
            GL.DetachShader(_shaderHandle, tesselationControlShader);
            GL.DeleteShader(tesselationControlShader);
        }

        if (tesselationEvaluationShader != 0)
        {
            GL.DetachShader(_shaderHandle, tesselationEvaluationShader);
            GL.DeleteShader(tesselationEvaluationShader);
        }

        if (computeShader != 0)
        {
            GL.DetachShader(_shaderHandle, computeShader);
            GL.DeleteShader(computeShader);
        }

        GL.GetProgram(_shaderHandle, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);
        for (var i = 0; i < numberOfUniforms; i++)
        {
            var key = GL.GetActiveUniform(_shaderHandle, i, out _, out _);
            var location = GL.GetUniformLocation(_shaderHandle, key);

            _uniformLocations.Add(key, location);
        }
    }

    public virtual void Unload()
    {
        if (_shaderHandle != 0)
        {
            GL.UseProgram(0);
            GL.DeleteProgram(_shaderHandle);
        }
    }

    private static void CompileShader(int shader)
    {
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
        if (code != (int)All.True) 
            throw new Exception($"Cant compile shader, {GL.GetShaderInfoLog(shader)}");
    }

    private static void LinkProgram(int program)
    {
        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
        if (code != (int)All.True) 
            throw new Exception($"Cant link shader, {GL.GetProgramInfoLog(program)}");
    }

    public virtual void Bind()
    {
        if (_shaderHandle != 0)
        {
            GL.UseProgram(_shaderHandle);
        }
        else
        {
            Log.Error("[Shader] Tried drawing shader with no handle!");
        }
    }

    public virtual void Unbind()
    {
        GL.UseProgram(0);
    }

    public int GetAttribLocation(string attribName)
    {
        return GL.GetAttribLocation(_shaderHandle, attribName);
    }

    private static string LoadSource(string path)
    {
        using var sr = new StreamReader(path, Encoding.UTF8);
        return sr.ReadToEnd();
    }

    /// <summary>
    ///     Set a uniform int on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetInt(string name, int data, bool bind = false)
    {
        if (!_uniformLocations.ContainsKey(name))
        {
            Log.Error("[Shader] Uniform {Name} isn't found!", name);
            return;
        }

        if (bind)
            Bind();

        GL.Uniform1(_uniformLocations[name], data);
    }

    /// <summary>
    ///     Set a uniform float on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetFloat(string name, float data, bool bind = false)
    {
        if (!_uniformLocations.ContainsKey(name))
        {
            Log.Error("[Shader] Uniform {Name} isn't found!", name);
            return;
        }

        if (bind)
            Bind();

        GL.Uniform1(_uniformLocations[name], data);
    }

    /// <summary>
    ///     Set a uniform Matrix4 on this shader
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="transpose"></param>
    /// <param name="bind"></param>
    public void SetMatrix4(string name, Matrix4 data, bool transpose = true, bool bind = false)
    {
        if (!_uniformLocations.ContainsKey(name))
        {
            Log.Error("[Shader] Uniform {Name} isn't found!", name);
            return;
        }

        if (bind)
            Bind();

        GL.UniformMatrix4(_uniformLocations[name], transpose, ref data);
    }

    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector2(string name, Vector2 data, bool bind = false)
    {
        if (!_uniformLocations.ContainsKey(name))
        {
            Log.Error("[Shader] Uniform {Name} isn't found!", name);
            return;
        }

        if (bind)
            Bind();

        GL.Uniform2(_uniformLocations[name], data);
    }

    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector3(string name, Vector3 data, bool bind = false)
    {
        if (!_uniformLocations.ContainsKey(name))
        {
            Log.Error("[Shader] Uniform {Name} isn't found!", name);
            return;
        }

        if (bind)
            Bind();

        GL.Uniform3(_uniformLocations[name], data);
    }

    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector3(string name, float[] data, bool bind = false)
    {
        if (!_uniformLocations.ContainsKey(name))
        {
            Log.Error("[Shader] Uniform {Name} isn't found!", name);
            return;
        }

        if (bind)
            Bind();

        GL.Uniform3(_uniformLocations[name], data.Length, data);
    }

    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector4(string name, Vector4 data, bool bind = false)
    {
        if (!_uniformLocations.ContainsKey(name))
        {
            Log.Error("[Shader] Uniform {Name} isn't found!", name);
            return;
        }

        if (bind)
            Bind();

        GL.Uniform4(_uniformLocations[name], data);
    }

    /// <summary>
    ///     Set a uniform Vector3 on this shader.
    /// </summary>
    /// <param name="name">The name of the uniform</param>
    /// <param name="data">The data to set</param>
    /// <param name="bind"></param>
    public void SetVector4(string name, float[] data, bool bind = false)
    {
        if (!_uniformLocations.ContainsKey(name))
        {
            Log.Error("[Shader] Uniform {Name} isn't found!", name);
            return;
        }

        if (bind)
            Bind();

        GL.Uniform3(_uniformLocations[name], data.Length, data);
    }
}