using Jellyfish.Input;
using Jellyfish.Render.Shaders;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish.Render.Screenspace;

public class Combine : ScreenspaceEffect, IInputHandler
{
    public Combine() : base(new TextureParams
    {
        Name = "_rt_Combined",
        WrapMode = TextureWrapMode.ClampToEdge,
        MinFiltering = TextureMinFilter.Linear,
        MagFiltering = TextureMagFilter.Linear,
        InternalFormat = SizedInternalFormat.Rgb8,
        RenderTargetParams = new RenderTargetParams
        {
            Width = Engine.MainViewport.Size.X,
            Heigth = Engine.MainViewport.Size.Y,
            Attachment = FramebufferAttachment.ColorAttachment0,
        }
    }, new PostProcessing())
    {
        Priority = 100; // must be as late as possible
        Engine.InputManager.RegisterInputHandler(this);
    }

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        if (keyboardState.IsKeyPressed(Keys.P))
        {
            var shader = (PostProcessing)Shader;
            shader.IsEnabled = !shader.IsEnabled;
            return true;
        }

        return false;
    }

    public override void Unload()
    {
        base.Unload();
        Engine.InputManager.UnregisterInputHandler(this);
    }
}