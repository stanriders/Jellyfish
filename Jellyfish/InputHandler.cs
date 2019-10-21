using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Jellyfish
{
    class InputHandler
    {
        public Camera Camera { get; }
        public bool IsControllingCursor { get; set; }

        private Vector2 lastMousePos;
        private bool firstMove = true;

        private bool wireframe;
        private bool wireframeLock;

        private const float camera_speed = 15.5f;
        private const float sensitivity = 0.2f;

        public InputHandler()
        {
            Camera = new Camera(Vector3.UnitZ * 3, 1280 / (float)720);
        }

        public void Frame(float frameTime)
        {
            var input = Keyboard.GetState();
            var mouseinput = Mouse.GetState();
            if (input.IsKeyDown(Key.Escape))
            {
                Environment.Exit(0);
            }
            
            if (mouseinput.IsButtonDown(MouseButton.Left))
            {
                if (input.IsKeyDown(Key.W))
                    Camera.Position += Camera.Front * camera_speed * frameTime; // Forward 
                if (input.IsKeyDown(Key.S))
                    Camera.Position -= Camera.Front * camera_speed * frameTime; // Backwards
                if (input.IsKeyDown(Key.A))
                    Camera.Position -= Camera.Right * camera_speed * frameTime; // Left
                if (input.IsKeyDown(Key.D))
                    Camera.Position += Camera.Right * camera_speed * frameTime; // Right
                if (input.IsKeyDown(Key.Space))
                    Camera.Position += Camera.Up * camera_speed * frameTime; // Up 
                if (input.IsKeyDown(Key.LShift))
                    Camera.Position -= Camera.Up * camera_speed * frameTime; // Down

                if (!IsControllingCursor)
                    IsControllingCursor = true;
            }
            else
            {
                if (IsControllingCursor)
                    IsControllingCursor = false;
            }

            if (input.IsKeyDown(Key.Q) && !wireframeLock)
            {
                wireframe = !wireframe;
                GL.PolygonMode(MaterialFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
                wireframeLock = true;
            }

            if (wireframeLock && input.IsKeyUp(Key.Q))
                wireframeLock = false;
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            var mouse = Mouse.GetState();
            if (IsControllingCursor)
            {
                Mouse.SetPosition(MainWindow.WindowX + MainWindow.WindowWidth / 2f, MainWindow.WindowY + MainWindow.WindowHeight / 2f);
                if (firstMove)
                {
                    lastMousePos = new Vector2(mouse.X, mouse.Y);
                    firstMove = false;
                }
                else
                {
                    var deltaX = mouse.X - lastMousePos.X;
                    var deltaY = mouse.Y - lastMousePos.Y;
                    lastMousePos = new Vector2(mouse.X, mouse.Y);

                    Camera.Yaw += deltaX * sensitivity;
                    Camera.Pitch -= deltaY * sensitivity;
                }
            }
            else
            {
                firstMove = true;
            }
        }
    }
}
