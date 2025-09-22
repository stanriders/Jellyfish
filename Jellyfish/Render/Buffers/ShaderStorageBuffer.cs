using OpenTK.Graphics.OpenGL;
using System;
using System.Runtime.InteropServices;
using Jellyfish.Render.Shaders.Structs;

namespace Jellyfish.Render.Buffers;

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
}