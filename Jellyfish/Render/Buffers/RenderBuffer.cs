using System;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.Render.Buffers;

public class RenderBuffer : IDisposable
{
    public readonly InternalFormat Type;
    public readonly int Handle;

    public RenderBuffer(InternalFormat type, int width, int height)
    {
        Type = type;
        Handle = GL.CreateRenderbuffer();
        GL.NamedRenderbufferStorage(Handle, type, width, height);
    }

    public void UpdateSize(int width, int heigth)
    {
        GL.NamedRenderbufferStorage(Handle, Type, width, heigth);
    }

    public void Dispose()
    {
        GL.DeleteRenderbuffer(Handle);
    }
}