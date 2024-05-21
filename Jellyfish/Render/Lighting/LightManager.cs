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

    private const int max_lights = 4;
    private static readonly List<Light> lights = new(max_lights);

    private const int shadow_size = 2048;
    public static void AddLight(ILightSource source)
    {
        if (lights.Count < max_lights)
        {
            var light = new Light
            {
                Source = source
            };

            if (source.UseShadows)
            {
                CreateShadows(light);
            }

            lights.Add(light);
        }
    }

    public static void DrawShadows()
    {
        foreach (var light in lights.Where(x=> x.Source.Enabled && x.Source.UseShadows))
        {
            if (light.Source.UseShadows && light.ShadowRt == null)
            {
                // create shadows lazily
                CreateShadows(light);
            }

            light.ShadowFrameBuffer!.Bind();

            GL.Viewport(0, 0, shadow_size, shadow_size);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            MeshManager.Draw(light.ShadowShader);

            light.ShadowFrameBuffer.Unbind();
        }
    }

    private static void CreateShadows(Light light)
    {
        var framebuffer = new FrameBuffer();
        framebuffer.Bind();

        var shader = new Shadow(light.Source);

        var rt = new RenderTarget($"_rt_Shadow{lights.Count}", shadow_size, shadow_size, PixelFormat.DepthComponent,
            FramebufferAttachment.DepthAttachment, PixelType.Float, TextureWrapMode.ClampToBorder, new[] { 1f, 1f, 1f, 1f });
        rt.Bind();

        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);
        framebuffer.Check();
        framebuffer.Unbind();

        light.ShadowFrameBuffer = framebuffer;
        light.ShadowShader = shader;
        light.ShadowRt = rt;
    }
}