
using System;
using Jellyfish.Entities;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using Jellyfish.Render;

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

            if (EntityManager.CreateEntity("npc_gman") is Gman gman)
            {
                gman.Rotation = new Vector3(-1.5f, 0, 0);
            }

            EntityManager.CreateEntity("bezierplane");

            if (EntityManager.CreateEntity("model_dynamic") is DynamicModel elite)
            {
                elite.Model = "Elite_reference.smd";
                elite.Position = new Vector3(50, 0, 0);
                elite.Rotation = new Vector3(-1.5f, 0, 0);
                elite.Load();
            }

            if (EntityManager.CreateEntity("model_dynamic") is DynamicModel police)
            {
                police.Model = "Police_reference.smd";
                police.Position = new Vector3(0, 0, 50);
                police.Rotation = new Vector3(-1.5f, 0, 0);
                police.Load();
            }

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
