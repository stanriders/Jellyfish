using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;
using Jellyfish.Render.Shaders.Structs;

namespace Jellyfish.Render.Buffers;

public class ShaderStorageBuffer
{
    public readonly int Handle;

    public ShaderStorageBuffer(string name, int size)
    {
        GL.CreateBuffer(out Handle);
        GL.ObjectLabel(ObjectIdentifier.Buffer, (uint)Handle, name.Length, name);

        GL.NamedBufferStorage(Handle, size, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);
        GL.NamedBufferSubData(Handle, IntPtr.Zero, size, IntPtr.Zero);
    }

    public void Bind(uint binding)
    {
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, binding, Handle);
    }

    public void Clear()
    {
        GL.ClearNamedBufferData(Handle,
            SizedInternalFormat.R32ui,
            PixelFormat.RedInteger,
            PixelType.UnsignedInt,
            IntPtr.Zero);
    }
    public void Unload()
    {
        GL.DeleteBuffer(Handle);
    }
}

public class ShaderStorageBuffer<T> where T: struct, IGpuStruct
{
    public readonly int Handle;

    public ShaderStorageBuffer(string name, T data)
    {
        GL.CreateBuffer(out Handle);
        GL.ObjectLabel(ObjectIdentifier.Buffer, (uint)Handle, name.Length, name);

        var bufferSize = Marshal.SizeOf<T>();
        GL.NamedBufferStorage(Handle, bufferSize, IntPtr.Zero, BufferStorageMask.DynamicStorageBit);

        var ptr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            Marshal.StructureToPtr(data, ptr, false);
            GL.NamedBufferSubData(Handle, IntPtr.Zero, bufferSize, ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public void Bind(uint binding)
    {
        GL.BindBufferBase(BufferTarget.ShaderStorageBuffer, binding, Handle);
    }

    public void UpdateData(T data)
    {
        var bufferSize = Marshal.SizeOf<T>();
        var ptr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            Marshal.StructureToPtr(data, ptr, false);
            GL.NamedBufferSubData(Handle, IntPtr.Zero, bufferSize, ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
    public void Unload()
    {
        GL.DeleteBuffer(Handle);
    }
}