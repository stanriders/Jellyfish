using Jellyfish.Debug;
using Jellyfish.Entities;
using Jellyfish.Render.Buffers;
using Jellyfish.Render.Shaders;
using Jellyfish.Render.Shaders.Structs;
using Jellyfish.Utils;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    public const int max_lights = 256;

    public readonly List<Light> Lights = new(max_lights);

    public readonly ShaderStorageBuffer<LightSources> LightSourcesSsbo = new("lightSourcesSSBO", new LightSources());

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

    public void UpdateShaderBuffer()
    {
        var lights = ArrayPool<Shaders.Structs.Light>.Shared.Rent(max_lights);
        var sunCascadeTextures = ArrayPool<ulong>.Shared.Rent(Entities.Sun.cascades);
        var sunProjections = ArrayPool<Matrix4>.Shared.Rent(Entities.Sun.cascades);
        var cascadeRangesFar = ArrayPool<int>.Shared.Rent(Entities.Sun.cascades);
        var cascadeRangesNear = ArrayPool<int>.Shared.Rent(Entities.Sun.cascades);

        var totalLights = Lights.Count;
        var lightSourcesStruct = new LightSources
        {
            Lights = lights,
            Sun = new Shaders.Structs.Sun { ShadowTexture = sunCascadeTextures },
            SunEnabled = Sun != null && Sun.Source.Enabled ? 1 : 0
        };

        var currentLight = 0;
        for (var i = 0; i < totalLights; i++)
        {
            var source = Lights[i].Source;
            if (!source.Enabled)
            {
                continue;
            }

            lightSourcesStruct.Lights[currentLight].Position = new Vector4(source.Position);

            var rotationVector = Vector3.Transform(-Vector3.UnitY, source.Rotation);
            lightSourcesStruct.Lights[currentLight].Direction = new Vector4(rotationVector);

            lightSourcesStruct.Lights[currentLight].Diffuse = new Vector4(source.Color.X, source.Color.Y, source.Color.Z, 0);

            lightSourcesStruct.Lights[currentLight].Brightness = source.Brightness;

            lightSourcesStruct.Lights[currentLight].Type = source switch
            {
                Spotlight => 1,
                PointLight => 0,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (source is PointLight point)
            {
                lightSourcesStruct.Lights[currentLight].Constant = point.GetPropertyValue<float>("Constant");
                lightSourcesStruct.Lights[currentLight].Linear = point.GetPropertyValue<float>("Linear");
                lightSourcesStruct.Lights[currentLight].Quadratic = point.GetPropertyValue<float>("Quadratic");
            }

            if (source is Spotlight spot)
            {
                lightSourcesStruct.Lights[currentLight].Constant = spot.GetPropertyValue<float>("Constant");
                lightSourcesStruct.Lights[currentLight].Linear = spot.GetPropertyValue<float>("Linear");
                lightSourcesStruct.Lights[currentLight].Quadratic = spot.GetPropertyValue<float>("Quadratic");
                lightSourcesStruct.Lights[currentLight].Cone = (float)Math.Cos(MathHelper.DegreesToRadians(spot.GetPropertyValue<float>("Cone")));
                lightSourcesStruct.Lights[currentLight].Outcone = (float)Math.Cos(MathHelper.DegreesToRadians(spot.GetPropertyValue<float>("OuterCone")));
            }

            lightSourcesStruct.Lights[currentLight].Near = source.NearPlane;
            lightSourcesStruct.Lights[currentLight].Far = source.FarPlane;

            lightSourcesStruct.Lights[currentLight].LightSpaceMatrix = source.Projection(0);

            lightSourcesStruct.Lights[currentLight].HasShadows = source.UseShadows && Lights[i].Shadows.Count > 0 ? 1 : 0;
            lightSourcesStruct.Lights[currentLight].UsePcss = source.UseShadows && source.UsePcss ? 1 : 0;

            if (source.UseShadows && Lights[i].Shadows.Count > 0)
            {
                lightSourcesStruct.Lights[currentLight].ShadowTexture = Lights[i].Shadows[0].BindlessHandle;
            }

            currentLight++;
        }

        lightSourcesStruct.LightsCount = currentLight;

        if (Sun != null && Sun.Source.Enabled)
        {
            var sun = Sun.Source;

            lightSourcesStruct.Sun.Diffuse = new Vector4(sun.Color.X, sun.Color.Y, sun.Color.Z, 0);

            var rotationVector = Vector3.Transform(-Vector3.UnitY, sun.Rotation);
            lightSourcesStruct.Sun.Direction = new Vector4(rotationVector);

            lightSourcesStruct.Sun.Brightness = sun.Brightness;

            for (var i = 0; i < Entities.Sun.cascades; i++)
            {
                sunProjections[i] = sun.Projection(i);
                cascadeRangesFar[i] = Entities.Sun.CascadeRanges[i].Far;
                cascadeRangesNear[i] = Entities.Sun.CascadeRanges[i].Near;
            }

            lightSourcesStruct.Sun.LightSpaceMatrix = sunProjections;
            lightSourcesStruct.Sun.CascadeFar = cascadeRangesFar;
            lightSourcesStruct.Sun.CascadeNear = cascadeRangesNear;

            lightSourcesStruct.Sun.HasShadows = sun.UseShadows && Sun.Shadows.Count > 0 ? 1 : 0;
            lightSourcesStruct.Sun.UsePcss = sun.UseShadows && sun.UsePcss ? 1 : 0;

            if (Sun.Source.UseShadows)
            {
                for (var i = 0; i < Entities.Sun.cascades; i++)
                {
                    lightSourcesStruct.Sun.ShadowTexture[i] = Sun.Shadows[i].BindlessHandle;
                }
            }
        }

        LightSourcesSsbo.UpdateData(lightSourcesStruct);

        ArrayPool<Shaders.Structs.Light>.Shared.Return(lights, true);
        ArrayPool<ulong>.Shared.Return(sunCascadeTextures);
        ArrayPool<Matrix4>.Shared.Return(sunProjections);
        ArrayPool<int>.Shared.Return(cascadeRangesNear);
        ArrayPool<int>.Shared.Return(cascadeRangesFar);
    }

    private void CreateShadow(Light light, string subname = "")
    {
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
            InternalFormat = SizedInternalFormat.DepthComponent32f,
            RenderTargetParams = new RenderTargetParams
            {
                Width = light.Source.ShadowResolution,
                Heigth = light.Source.ShadowResolution,
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