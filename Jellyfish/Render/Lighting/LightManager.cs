using Jellyfish.Debug;
using Jellyfish.Entities;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Jellyfish.Render.Shaders.Structs;
using Sun = Jellyfish.Entities.Sun;

namespace Jellyfish.Render.Lighting;

public class LightManager
{
    public class Light
    {
        public ILightSource Source { get; set; } = null!;
        public List<Shadow> Shadows { get; set; } = new();

        public class Shadow
        {
            public required Texture RenderTarget { get; set; }
            public required FrameBuffer FrameBuffer { get; set; }
            public required Shaders.Shadow Shader { get; set; }
            public ulong BindlessHandle { get; set; }
        }
    }

    public Light? Sun { get; private set; }

    public const int max_lights = 16;
    public const int max_shadows = 16;

    public readonly List<Light> Lights = new(max_lights);

    public readonly ShaderStorageBuffer<LightSources> LightSourcesSsbo;

    public LightManager()
    {
        LightSourcesSsbo = new ShaderStorageBuffer<LightSources>("lightSourcesSSBO", new LightSources());
    }

    public void AddLight(ILightSource source)
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

        if (Lights.Count < max_lights)
        {
            Lights.Add(new Light
            {
                Source = source
            });
        }
    }

    public void RemoveLight(ILightSource source)
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

        var light = Lights.Find(x => x.Source == source);
        if (light != null)
        {
            foreach (var shadow in light.Shadows)
            {
                shadow.FrameBuffer.Unload();
                shadow.RenderTarget.Unload();
                shadow.Shader.Unload();
            }

            Lights.Remove(light);
        }
    }

    public void DrawShadows()
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
                Engine.MeshManager.Draw(false, shadow.Shader, new Frustum(Sun.Source.Projection(i)));

                shadow.FrameBuffer.Unbind();
            }
        }

        foreach (var light in Lights.Where(x=> x.Source.Enabled))
        {
            // create shadows lazily
            if (light.Source.UseShadows && light.Shadows.Count == 0)
            {
                for (var i = 0; i < light.Source.ProjectionCount; i++)
                {
                    CreateShadow(light, i.ToString());
                }
            }

            if (!light.Source.UseShadows && light.Shadows.Any())
            {
                DestroyShadows(light);
                continue;
            }

            foreach (var shadow in light.Shadows)
            {
                Frustum? frustum = null;
                if (light.Source is IHaveFrustum frustumEntity)
                {
                    frustum = frustumEntity.GetFrustum();
                    if (!Engine.MainViewport.GetFrustum().IsInside(frustum.Value))
                    {
                        frustum?.Dispose();
                        continue;
                    }
                }
                else
                {
                    if (!Engine.MainViewport.GetFrustum().IsInside(light.Source.Position, light.Source.FarPlane))
                    {
                        frustum?.Dispose();
                        continue;
                    }
                }

                shadow.FrameBuffer.Bind();

                GL.Viewport(0, 0, light.Source.ShadowResolution, light.Source.ShadowResolution);
                GL.ClearDepth(1.0);
                GL.Clear(ClearBufferMask.DepthBufferBit);

                Engine.MeshManager.Draw(false, shadow.Shader, frustum);

                shadow.FrameBuffer.Unbind();
                frustum?.Dispose();
            }
        }

        GL.CullFace(TriangleFace.Back);
        GL.Enable(EnableCap.CullFace);
        PerformanceMeasurment.Add("LightManager.DrawShadows", stopwatch.Elapsed.TotalMilliseconds);
    }

    private void CreateShadow(Light light, string subname = "")
    {
        if (Lights.Sum(x => x.Shadows.Count) >= max_shadows)
            return;

        var framebuffer = new FrameBuffer();
        framebuffer.Bind();

        var shader = new Shadow(light.Source, light.Shadows.Count);

        var rt = Engine.TextureManager.CreateTexture(new TextureParams
        {
            Name = $"_rt_Shadow{Lights.IndexOf(light)}{subname}",
            BorderColor = [1f, 1f, 1f, 1f],
            WrapMode = TextureWrapMode.ClampToBorder,
            MinFiltering = TextureMinFilter.Linear,
            MagFiltering = TextureMagFilter.Linear,
            RenderTargetParams = new RenderTargetParams
            {
                Width = light.Source.ShadowResolution,
                Heigth = light.Source.ShadowResolution,
                InternalFormat = SizedInternalFormat.DepthComponent32f,
                Attachment = FramebufferAttachment.DepthAttachment,
            }
        });

        GL.BindTexture(TextureTarget.Texture2d, rt.Handle);

        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);
        framebuffer.Check();
        framebuffer.Unbind();

        var bindlessHandle = GL.ARB.GetTextureHandleARB(rt.Handle);
        GL.ARB.MakeTextureHandleResidentARB(bindlessHandle);

        light.Shadows.Add(new Light.Shadow
        {
            FrameBuffer = framebuffer,
            Shader = shader,
            RenderTarget = rt,
            BindlessHandle = bindlessHandle
        });
    }

    private void DestroyShadows(Light light)
    {
        if (light.Shadows.Count == 0)
            return;

        foreach (var shadow in light.Shadows)
        {
            shadow.RenderTarget.Unload();
            shadow.FrameBuffer.Unload();
            shadow.Shader.Unload();
            GL.ARB.MakeTextureHandleNonResidentARB(shadow.BindlessHandle);
        }

        light.Shadows.Clear();
    }
}