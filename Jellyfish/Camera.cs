using System;
using Jellyfish.Entities;
using OpenTK.Mathematics;

namespace Jellyfish;

public class Camera
{
    private static Vector3 front = -Vector3.UnitZ;

    private static float pitch;
    private static float yaw = -MathHelper.PiOver2; // Without this you would be started rotated 90 degrees right
    private static float fov = MathHelper.PiOver2;

    private PointLight _camLight;

    public Camera(Vector3 position, float aspectRatio)
    {
        Position = position;
        AspectRatio = aspectRatio;
    }

    // The position of the camera
    public static Vector3 Position { get; set; }

    public static float AspectRatio { private get; set; }

    public static Vector3 Front => front;
    public static Vector3 Up { get; private set; } = Vector3.UnitY;

    public static Vector3 Right { get; private set; } = Vector3.UnitX;

    public float Pitch
    {
        get => MathHelper.RadiansToDegrees(pitch);
        set
        {
            // We clamp the pitch value between -89 and 89 to prevent the camera from going upside down
            var angle = MathHelper.Clamp(value, -89f, 89f);
            pitch = MathHelper.DegreesToRadians(angle);
            UpdateVectors();
        }
    }

    public float Yaw
    {
        get => MathHelper.RadiansToDegrees(yaw);
        set
        {
            yaw = MathHelper.DegreesToRadians(value);
            UpdateVectors();
        }
    }

    public float Fov
    {
        get => MathHelper.RadiansToDegrees(fov);
        set
        {
            var angle = MathHelper.Clamp(value, 1f, 45f);
            fov = MathHelper.DegreesToRadians(angle);
        }
    }

    public static Matrix4 GetViewMatrix()
    {
        return Matrix4.LookAt(Position, Position + front, Up);
    }

    public static Matrix4 GetProjectionMatrix()
    {
        return Matrix4.CreatePerspectiveFieldOfView(fov, AspectRatio, 0.05f, 1000f);
    }

    private void UpdateVectors()
    {
        front.X = (float)Math.Cos(pitch) * (float)Math.Cos(yaw);
        front.Y = (float)Math.Sin(pitch);
        front.Z = (float)Math.Cos(pitch) * (float)Math.Sin(yaw);

        front = Vector3.Normalize(front);
        Right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
        Up = Vector3.Normalize(Vector3.Cross(Right, front));

        if (_camLight is null)
        {
            _camLight = EntityManager.CreateEntity("light_point") as PointLight;
            if (_camLight != null)
            {
                _camLight.Enabled = true;
                _camLight.Color = new Color4(200, 220, 255, 100);
                _camLight.Load();
            }
        }
        else
        {
            _camLight.Position = Position;
        }
    }
}