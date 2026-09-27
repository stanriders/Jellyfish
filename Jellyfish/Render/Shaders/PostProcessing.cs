using Jellyfish.Console;
using Jellyfish.Render.Buffers;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Render.Shaders;

public class PostprocessingEnabled() : ConVar<bool>("mat_postprocess_enabled", true, Keys.P);
public class PostprocessingExposureKey() : ConVar<float>("mat_postprocess_exposure_key", 0.2f);
public class PostProcessing : Shader
{
    private class Compute() : Shader("shaders/PostProcessing.comp")
    {
        private const int histogram_bins = 128;

        private readonly ShaderStorageBuffer _histogram = new("histogramSSBO", (histogram_bins + 1) * sizeof(uint));
        private readonly Texture _rtColor = Engine.TextureManager.GetTexture("_rt_Color")!;
        private readonly Texture _rtExposure = Engine.TextureManager.GetTexture("_rt_Exposure")!;

        public override void Bind()
        {
            base.Bind();

            BindTexture(0, _rtColor);
            GL.BindImageTexture(2, _rtExposure.Handle, 0, false, 0, BufferAccess.ReadWrite, InternalFormat.R32f);

            _histogram.Clear();
            _histogram.Bind(0);

            SetVector2("screenSize", _rtColor.Size);
            SetFloat("minLogLum", -8.0f);
            SetFloat("maxLogLum", 8.0f);
            SetFloat("key", ConVarStorage.Get<float>("mat_postprocess_exposure_key"));
            SetFloat("adaptRate", 0.025f);
            SetFloat("percentile", 0.8f);
        }

        public void Dispatch()
        {
            DispatchCompute(((uint)_rtColor.Size.X + 15) / 16, ((uint)_rtColor.Size.Y + 15) / 16, 1,
                MemoryBarrierMask.TextureFetchBarrierBit | MemoryBarrierMask.ShaderImageAccessBarrierBit);
        }

        public override void Unload()
        {
            _rtColor.Unload();
            _rtExposure.Unload();
            _histogram.Dispose();

            base.Unload();
        }
    }

    private readonly Texture _rtColor;
    private readonly Texture _rtDepth;
    private readonly Texture _rtAmbientOcclusion;
    private readonly Texture _rtBloom;
    private readonly Texture _rtExposure;

    private readonly Compute _computeShader;

    public PostProcessing() :
        base("shaders/Screenspace.vert", null, "shaders/PostProcessing.frag")
    {
        _rtColor = Engine.TextureManager.GetTexture("_rt_Color")!;
        _rtDepth = Engine.TextureManager.GetTexture("_rt_Depth")!;
        _rtAmbientOcclusion = Engine.TextureManager.GetTexture("_rt_GtaoBlurY")!;
        _rtBloom = Engine.TextureManager.GetTexture("_rt_Bloom")!;

        _rtExposure = Engine.TextureManager.CreateTexture(new RenderTargetParams
        {
            Width = 1,
            Heigth = 1,
            Attachment = null,
            TextureParams = new TextureParams
            {
                Name = "_rt_Exposure",
                WrapMode = TextureWrapMode.ClampToEdge,
                MinFiltering = TextureMinFilter.Nearest,
                MagFiltering = TextureMagFilter.Nearest,
                InternalFormat = SizedInternalFormat.R32f
            }
        });
        GL.ClearTexImage(_rtExposure.Handle, 0, PixelFormat.Red, PixelType.Float, 1f);

        _computeShader = new Compute();
    }

    public override void Bind()
    {
        var isEnabled = ConVarStorage.Get<bool>("mat_postprocess_enabled");

        _computeShader.Bind();
        _computeShader.Dispatch();
        _computeShader.Unbind();

        base.Bind();

        BindTexture(0, _rtColor);
        BindTexture(1, _rtAmbientOcclusion);
        BindTexture(2, _rtBloom);
        //BindTexture(3, _rtDepth);
        BindTexture(4, _rtExposure);

        SetInt("isEnabled", isEnabled ? 1 : 0);
        SetFloat("bloomStrength", ConVarStorage.Get<float>("mat_bloom_strength"));
        SetInt("toneMappingMode", 2);
        //SetVector2("uCameraParams", new Vector2(Engine.MainViewport.NearPlane, Engine.MainViewport.FarPlane));
    }

    public override void Unload()
    {
        _rtColor.Unload();
        _rtAmbientOcclusion.Unload();
        _rtBloom.Unload();
        _rtDepth.Unload();
        _rtExposure.Unload();
        _computeShader.Unload();

        base.Unload();
    }
}