using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Jellyfish;

public class InputHandler
{
    private const float camera_speed = 16.0f;
    private const float sensitivity = 0.2f;

    private bool _wireframe;

    public InputHandler()
    {
        Camera = new Camera(Vector3.UnitZ * 3, MainWindow.WindowWidth / (float)MainWindow.WindowHeight);
    }

    public Camera Camera { get; }
    public bool IsControllingCursor { get; set; }

    public void Frame(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        var input = keyboardState;
        var mouseinput = mouseState;
        if (input.IsKeyDown(Keys.Escape)) Environment.Exit(0);

        var cameraSpeed = input.IsKeyDown(Keys.LeftShift) ? camera_speed * 4 : camera_speed;

        if (mouseinput.IsButtonDown(MouseButton.Left))
        {
            if (input.IsKeyDown(Keys.W))
                Camera.Position += Camera.Front * cameraSpeed * frameTime; // Forward 
            if (input.IsKeyDown(Keys.S))
                Camera.Position -= Camera.Front * cameraSpeed * frameTime; // Backwards
            if (input.IsKeyDown(Keys.A))
                Camera.Position -= Camera.Right * cameraSpeed * frameTime; // Left
            if (input.IsKeyDown(Keys.D))
                Camera.Position += Camera.Right * cameraSpeed * frameTime; // Right
            if (input.IsKeyDown(Keys.Space))
                Camera.Position += Camera.Up * cameraSpeed * frameTime; // Up 
            if (input.IsKeyDown(Keys.LeftControl))
                Camera.Position -= Camera.Up * cameraSpeed * frameTime; // Down

            if (!IsControllingCursor)
                IsControllingCursor = true;
        }
        else
        {
            if (IsControllingCursor)
                IsControllingCursor = false;
        }

        if (input.IsKeyPressed(Keys.Q))
        {
            _wireframe = !_wireframe;
            GL.PolygonMode(MaterialFace.FrontAndBack, _wireframe ? PolygonMode.Line : PolygonMode.Fill);
        }
    }

    public void OnMouseMove(MouseMoveEventArgs e)
    {
        if (IsControllingCursor)
        {
            Camera.Yaw += e.DeltaX * sensitivity;
            Camera.Pitch -= e.DeltaY * sensitivity;
        }
    }
}