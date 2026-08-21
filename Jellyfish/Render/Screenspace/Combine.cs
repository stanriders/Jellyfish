using Jellyfish.Render.Shaders;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Screenspace;

public class Combine : ScreenspaceEffect
{
    public Combine() : base(new RenderTargetParams
    {
        Width = Engine.MainViewport.Size.X,
        Heigth = Engine.MainViewport.Size.Y,
        Attachment = FramebufferAttachment.ColorAttachment0,
        TextureParams = new TextureParams
        {
            Name = "_rt_Combined",
            WrapMode = TextureWrapMode.ClampToEdge,
            MinFiltering = TextureMinFilter.Linear,
            MagFiltering = TextureMagFilter.Linear,
            InternalFormat = SizedInternalFormat.Rgb8
        }
    }, new PostProcessing())
    {
        Priority = 100; // must be as late as possible
    }
}