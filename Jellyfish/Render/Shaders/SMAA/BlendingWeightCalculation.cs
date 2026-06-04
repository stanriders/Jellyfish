using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders.SMAA;

public class BlendingWeightCalculation : Shader
{
    private readonly Texture _rtEdges;
    private readonly Texture _areaTexture;
    private readonly Texture _searchTexture;
    public BlendingWeightCalculation() 
        : base("shaders/BlendingWeightCalculation.vert", null, "shaders/BlendingWeightCalculation.frag")
    {
        _rtEdges = Engine.TextureManager.GetTexture("_rt_SMAAEdgeDetection")!;
        _areaTexture = Engine.TextureManager.GetTexture(new TextureParams
        {
            Name = "materials/engine/smaa_area.dds", 
            MaxLevels = 1, 
            MagFiltering = TextureMagFilter.Linear, 
            MinFiltering = TextureMinFilter.Linear, 
            WrapMode = TextureWrapMode.ClampToEdge,
            Srgb = false,
            InternalFormat = SizedInternalFormat.Rgb8,
            PixelFormat = PixelFormat.Rgb,
        }).Texture;

        _searchTexture = Engine.TextureManager.GetTexture(new TextureParams 
        { 
            Name = "materials/engine/smaa_search.dds",
            MaxLevels = 1,
            MagFiltering = TextureMagFilter.Linear,
            MinFiltering = TextureMinFilter.Linear,
            WrapMode = TextureWrapMode.ClampToEdge,
            Srgb = false,
            InternalFormat = SizedInternalFormat.R8,
            PixelFormat = PixelFormat.Red
        }).Texture;
    }

    public override void Bind()
    {
        base.Bind();

        BindTexture(0, _rtEdges);
        BindTexture(1, _areaTexture);
        BindTexture(2, _searchTexture);

        SetVector2("uViewportSize", Engine.MainViewport.Size);
        SetVector2("uTexelSize", 1.0f / (Vector2)Engine.MainViewport.Size);
    }

    public override void Unload()
    {
        _rtEdges.Unload();
        _areaTexture.Unload();
        _searchTexture.Unload();

        base.Unload();
    }
}