
using System;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using Jellyfish.Render;
using Jellyfish.Render.Lighting;

namespace Jellyfish
{
    public class MainWindow : GameWindow
    {
        public static int WindowX { get; set; }
        public static int WindowY { get; set; }
        public static int WindowWidth { get; set; }
        public static int WindowHeight { get; set; }

        private InputHandler inputHandler;
        private OpenGLRender render;
        private PointLight playerLight;

        public MainWindow(int width, int height, string title) : base(width, height,
            new GraphicsMode(ColorFormat.Empty, 16), title)
        {
            WindowHeight = height;
            WindowWidth = width;
        }

        protected override void OnLoad(EventArgs e)
        {
            render = new OpenGLRender();
            inputHandler = new InputHandler();

            MapParser.Parse("maps/test.yml");

            playerLight = new PointLight()
            {
                Color = new Color4(0,255,128, 255),
                Enabled = true,
                Quadratic = 0.9f,
                Linear = 0.1f,
                Constant = 0.0f
            };
            LightManager.AddLight(playerLight);

            base.OnLoad(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            render.Frame();
            
            SwapBuffers();

            base.OnRenderFrame(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!Focused)
                return;

            EntityManager.Frame();

            WindowX = X;
            WindowY = Y;

            inputHandler.Frame((float)e.Time);
            CursorVisible = !inputHandler.IsControllingCursor;

            playerLight.Position = Camera.Position;

            base.OnUpdateFrame(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            if (!Focused)
                return;

            inputHandler.OnMouseMove(e);

            base.OnMouseMove(e);
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);

            WindowHeight = Height;
            WindowWidth = Width;

            base.OnResize(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            EntityManager.Unload();
            render.Unload();

            base.OnUnload(e);
        }
    }
}
