using Jellyfish.Debug;
using Jellyfish.Entities;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Jellyfish.Render.Lighting;

public static class LightManager
{
    public class Light
    {
        public ILightSource Source { get; set; } = null!;
        public List<Shadow> Shadows { get; set; } = new();

        public class Shadow
        {
            public required RenderTarget RenderTarget { get; set; }
            public required FrameBuffer FrameBuffer { get; set; }
            public required Shaders.Shadow Shader { get; set; }
        }

    }

    public static IReadOnlyList<Light> Lights => lights.AsReadOnly();
    public static Light? Sun { get; private set; }

    public const int max_lights = 12;
    private static readonly List<Light> lights = new(max_lights);

    public static void AddLight(ILightSource source)
    {
        if (source is Sun)
        {
            Sun = new Light { Source = source };
            for (var i = 0; i < Entities.Sun.cascades; i++)
            {
                CreateShadow(Sun, $"cascade{i}");
            }
            return;
        }

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
        if (source is Sun && Sun != null)
        {
            foreach (var shadow in Sun.Shadows)
            {
                shadow.FrameBuffer.Unload();
                shadow.RenderTarget.Unload();
                shadow.Shader.Unload();
            }

            Sun = null;
            return;
        }

        var light = lights.Find(x => x.Source == source);
        if (light != null)
        {
            foreach (var shadow in light.Shadows)
            {
                shadow.FrameBuffer.Unload();
                shadow.RenderTarget.Unload();
                shadow.Shader.Unload();
            }

            lights.Remove(light);
        }
    }

    public static void DrawShadows()
    {
        var stopwatch = Stopwatch.StartNew();
        GL.Disable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Front);

        if (Sun != null && Sun.Source.Enabled && Sun.Source.UseShadows)
        {
            for (var i = 0; i < Sun.Shadows.Count; i++)
            {
                var shadow = Sun.Shadows[i];
                shadow.FrameBuffer.Bind();

                GL.Viewport(0, 0, Sun.Source.ShadowResolution, Sun.Source.ShadowResolution);
                GL.Clear(ClearBufferMask.DepthBufferBit);
                Engine.MeshManager.Draw(false, shadow.Shader, new Frustum(Sun.Source.Projections[i]));

                shadow.FrameBuffer.Unbind();
            }
        }

        foreach (var light in lights.Where(x=> x.Source.Enabled && x.Source.UseShadows))
        {
            // create shadows lazily
            if (light.Shadows.Count == 0)
            {
                CreateShadow(light);
            }

            foreach (var shadow in light.Shadows)
            {
                if (!Engine.MainViewport.GetFrustum().IsInside(light.Source.Position, light.Source.FarPlane))
                    continue;

                shadow.FrameBuffer.Bind();

                GL.Viewport(0, 0, light.Source.ShadowResolution, light.Source.ShadowResolution);
                GL.Clear(ClearBufferMask.DepthBufferBit);

                Frustum? frustum = null;
                if (light.Source is IHaveFrustum frustumEntity)
                    frustum = frustumEntity.GetFrustum();

                Engine.MeshManager.Draw(false, shadow.Shader, frustum);

                shadow.FrameBuffer.Unbind();
            }
        }

        GL.CullFace(TriangleFace.Back);
        GL.Enable(EnableCap.CullFace);
        PerformanceMeasurment.Add("LightManager.DrawShadows", stopwatch.Elapsed.TotalMilliseconds);
    }

    private static void CreateShadow(Light light, string subname = "")
    {
        var framebuffer = new FrameBuffer();
        framebuffer.Bind();

        var shader = new Shadow(light.Source, light.Shadows.Count);

        var rt = new RenderTarget($"_rt_Shadow{lights.IndexOf(light)}{subname}", light.Source.ShadowResolution, light.Source.ShadowResolution, 
            SizedInternalFormat.DepthComponent32f, FramebufferAttachment.DepthAttachment, TextureWrapMode.ClampToBorder, [1f, 1f, 1f, 1f]);

        GL.BindTexture(TextureTarget.Texture2d, rt.TextureHandle);

        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);
        framebuffer.Check();
        framebuffer.Unbind();

        light.Shadows.Add(new Light.Shadow
        {
            FrameBuffer = framebuffer,
            Shader = shader,
            RenderTarget = rt
        });
    }
}