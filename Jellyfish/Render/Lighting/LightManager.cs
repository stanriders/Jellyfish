using System.Collections.Generic;
using System.Linq;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Lighting;

public static class LightManager
{
    public class Light
    {
        public ILightSource Source { get; set; } = null!;
        public RenderTarget? ShadowRt { get; set; }
        public FrameBuffer? ShadowFrameBuffer { get; set; }
        public Shadow? ShadowShader { get; set; }
    }

    public static IReadOnlyList<Light> Lights => lights.AsReadOnly();

    public const int max_lights = 4;
    private static readonly List<Light> lights = new(max_lights);

    public static void AddLight(ILightSource source)
    {
        if (lights.Count < max_lights)
        {
            lights.Add(new Light
            {
                Source = source
            });
        }
    }

    public static void RemoveLight(ILightSource source)
    {
        var light = lights.Find(x => x.Source == source);
        if (light != null)
        {
            light.ShadowFrameBuffer?.Unload();
            light.ShadowRt?.Unload();
            light.ShadowShader?.Unload();

            lights.Remove(light);
        }
    }

    public static void DrawShadows()
    {
        GL.CullFace(TriangleFace.Front);
        foreach (var light in lights.Where(x=> x.Source.Enabled && x.Source.UseShadows))
        {
            if (light.Source.UseShadows && light.ShadowRt == null)
            {
                // create shadows lazily
                CreateShadows(light);
            }

            light.ShadowFrameBuffer!.Bind();

            GL.Viewport(0, 0, light.Source.ShadowResolution, light.Source.ShadowResolution);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            MeshManager.Draw(false, light.ShadowShader);

            light.ShadowFrameBuffer.Unbind();
        }

        GL.CullFace(TriangleFace.Back);
    }

    private static void CreateShadows(Light light)
    {
        var framebuffer = new FrameBuffer();
        framebuffer.Bind();

        var shader = new Shadow(light.Source);

        var rt = new RenderTarget($"_rt_Shadow{lights.IndexOf(light)}", light.Source.ShadowResolution, light.Source.ShadowResolution, SizedInternalFormat.DepthComponent24, FramebufferAttachment.DepthAttachment, TextureWrapMode.ClampToBorder, [1f, 1f, 1f, 1f], true);
        rt.Bind(0);

        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);
        framebuffer.Check();
        framebuffer.Unbind();

        light.ShadowFrameBuffer = framebuffer;
        light.ShadowShader = shader;
        light.ShadowRt = rt;
    }
}