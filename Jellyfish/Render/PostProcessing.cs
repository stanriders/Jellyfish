using Jellyfish.Debug;
using Jellyfish.Input;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;
using Jellyfish.Utils;

namespace Jellyfish.Render;

public class PostProcessing : IInputHandler
{
    private readonly RenderTarget _rtColor;
    private readonly Shaders.PostProcessing _shader;
    private readonly VertexArray _vertexArray;
    private readonly VertexBuffer _vertexBuffer;

    private bool _isEnabled = true;
    private static float sceneExposure = 1.0f;
    private const float adj_speed = 0.05f;

    public PostProcessing(RenderTarget color)
    {
        _rtColor = color;
        _vertexBuffer = new VertexBuffer("PostProcessingQuad", CommonShapes.Quad, 4 * sizeof(float));
        _vertexArray = new VertexArray(_vertexBuffer, null);

        _shader = new Shaders.PostProcessing(color);

        var vertexLocation = _shader.GetAttribLocation("aPos");
        if (vertexLocation != null)
        {
            GL.EnableVertexArrayAttrib(_vertexArray.Handle, vertexLocation.Value);
            GL.VertexArrayAttribFormat(_vertexArray.Handle, vertexLocation.Value, 2, VertexAttribType.Float, false, 0);
            GL.VertexArrayAttribBinding(_vertexArray.Handle, vertexLocation.Value, 0);
        }

        var texCoordLocation = _shader.GetAttribLocation("aTexCoords");
        if (texCoordLocation != null)
        {
            GL.EnableVertexArrayAttrib(_vertexArray.Handle, texCoordLocation.Value);
            GL.VertexArrayAttribFormat(_vertexArray.Handle, texCoordLocation.Value, 2, VertexAttribType.Float, false,
                2 * sizeof(float));
            GL.VertexArrayAttribBinding(_vertexArray.Handle, texCoordLocation.Value, 0);
        }

        InputManager.RegisterInputHandler(this);
    }

    public void Draw()
    {
        var stopwatch = Stopwatch.StartNew();
        GL.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);

        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        _shader.Bind();
        _shader.SetVector2("screenSize", new Vector2(MainWindow.WindowWidth, MainWindow.WindowHeight));
        _shader.SetInt("isEnabled", _isEnabled ? 1 : 0);

        if (_isEnabled)
        {
            GL.GenerateTextureMipmap(_rtColor.TextureHandle); // TODO: This generates mipmaps every frame, replace with a histogram calculation
            var luminescence = new Span<float>(new float[128]); // can't allocate less than 128
            GL.GetTextureImage(_rtColor.TextureHandle, _rtColor.Levels - 1, PixelFormat.Rgb, PixelType.Float, luminescence.Length, luminescence);

            // TODO: histogram-based luminance
            var luminance = 0.2126f * luminescence[0] + 0.7152f * luminescence[1] + 0.0722f * luminescence[2]; // Calculate a weighted average
            luminance = Math.Max(luminance, 0.00001f);

            if (!double.IsNaN(luminance))
            {
                sceneExposure = float.Lerp(sceneExposure, 0.5f / luminance * 0.5f, adj_speed);
                sceneExposure = Math.Clamp(sceneExposure, 0.1f, 1f);
            }

            _shader.SetFloat("exposure", sceneExposure);
        }

        _vertexArray.Bind();

        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        _vertexArray.Unbind();
        _shader.Unbind();

        PerformanceMeasurment.Add("PostProcessing.Draw", stopwatch.Elapsed.TotalMilliseconds);
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (keyboardState.IsKeyPressed(Keys.P))
        {
            _isEnabled = !_isEnabled;
            return true;
        }

        return false;
    }

    public void Unload()
    {
        _shader.Unload();
        _vertexArray.Unload();
        _vertexBuffer.Unload();

        InputManager.UnregisterInputHandler(this);
    }
}