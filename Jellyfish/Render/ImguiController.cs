﻿using ImGuiNET;
using System;
using OpenTK.Graphics.OpenGL;
using Jellyfish.Render.Buffers;
using System.Runtime.CompilerServices;
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;
using Serilog;
using ErrorCode = OpenTK.Graphics.OpenGL.ErrorCode;

namespace Jellyfish.Render;

public sealed class ImguiController : IDisposable
{
    private bool _frameBegun;
    private int _fontTexture;

    private Imgui _shader = null!;
    private VertexArray _vao = null!;
    private VertexBuffer _vbo = null!;
    private IndexBuffer _ibo = null!;

    private int _windowWidth;
    private int _windowHeight;

    public ImguiController()
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void CreateDeviceResources()
    {
        _vao = new VertexArray();
        _vbo = new VertexBuffer(usage: BufferUsageHint.DynamicDraw);
        _ibo = new IndexBuffer(usage: BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();

        _shader = new Imgui();

        var stride = Unsafe.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        CheckGlError("End of ImGui setup");
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    public void RecreateFontDeviceTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out nint pixels, out var width, out var height, out _);

        var mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

        var prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        var prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, width, height);
        //LabelObject(ObjectLabelIdentifier.Texture, _fontTexture, "ImGui Text Atlas");

        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte,
            pixels);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mips - 1);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

        // Restore state
        GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);

        io.Fonts.SetTexID(_fontTexture);

        io.Fonts.ClearTexData();
    }

    public void Update(int windowWidth, int windowHeigth)
    {
        _windowWidth = windowWidth;
        _windowHeight = windowHeigth;

        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(_windowWidth, _windowHeight);

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void Render()
    {
        if (!_frameBegun)
            return;

        _frameBegun = false;
        ImGui.Render();

        var drawData = ImGui.GetDrawData();
        if (drawData.CmdListsCount == 0)
        {
            return;
        }

        // Get intial state.
        var prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
        var prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
        var prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
        var prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
        var prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
        var prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
        var prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
        var prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
        var prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
        var prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
        GL.ActiveTexture(TextureUnit.Texture0);
        Span<int> prevScissorBox = stackalloc int[4];
        unsafe
        {
            fixed (int* iptr = &prevScissorBox[0])
            {
                GL.GetInteger(GetPName.ScissorBox, iptr);
            }
        }

        Span<int> prevPolygonMode = stackalloc int[2];
        unsafe
        {
            fixed (int* iptr = &prevPolygonMode[0])
            {
                GL.GetInteger(GetPName.PolygonMode, iptr);
            }
        }

        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

        _vao.Bind();
        _vbo.Bind();

        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdLists[i];

            var vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vbo.Size)
            {
                var newSize = (int)Math.Max(_vbo.Size * 1.5f, vertexSize);
                _vbo.Size = newSize;
                Log.Information($"Resized dear imgui vertex buffer to new size {_vbo.Size}");
            }

            var indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _ibo.Size)
            {
                var newSize = (int)Math.Max(_ibo.Size * 1.5f, indexSize);
                _ibo.Size = newSize;
                Log.Information($"Resized dear imgui index buffer to new size {_ibo.Size}");
            }
        }

        // Setup orthographic projection matrix into our constant buffer
        var io = ImGui.GetIO();
        var mvp = Matrix4.CreateOrthographicOffCenter(
            0.0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);

        _shader.Draw();
        _shader.SetMatrix4("projection_matrix", mvp, false, false);
        _shader.SetInt("in_fontTexture", 0, false);
        CheckGlError("Projection");

        _vao.Bind();
        CheckGlError("VAO");

        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        // Render command lists
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];

            GL.BufferSubData(BufferTarget.ArrayBuffer, nint.Zero,
                cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmdList.VtxBuffer.Data);
            CheckGlError($"Data Vert {n}");

            GL.BufferSubData(BufferTarget.ElementArrayBuffer, nint.Zero, cmdList.IdxBuffer.Size * sizeof(ushort),
                cmdList.IdxBuffer.Data);
            CheckGlError($"Data Idx {n}");

            for (var cmd_i = 0; cmd_i < cmdList.CmdBuffer.Size; cmd_i++)
            {
                var pcmd = cmdList.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != nint.Zero)
                {
                    throw new NotImplementedException();
                }

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);
                CheckGlError("Texture");

                // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                var clip = pcmd.ClipRect;
                GL.Scissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X),
                    (int)(clip.W - clip.Y));
                CheckGlError("Scissor");

                if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                {
                    GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount,
                        DrawElementsType.UnsignedShort, (nint)(pcmd.IdxOffset * sizeof(ushort)),
                        unchecked((int)pcmd.VtxOffset));
                }
                else
                {
                    GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort,
                        (int)pcmd.IdxOffset * sizeof(ushort));
                }

                CheckGlError("Draw");
            }
        }

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);

        // Reset state
        GL.BindTexture(TextureTarget.Texture2D, 0);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.UseProgram(0);
        GL.BindVertexArray(0);
        GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb,
            (BlendEquationMode)prevBlendEquationAlpha);
        GL.BlendFuncSeparate(
            (BlendingFactorSrc)prevBlendFuncSrcRgb,
            (BlendingFactorDest)prevBlendFuncDstRgb,
            (BlendingFactorSrc)prevBlendFuncSrcAlpha,
            (BlendingFactorDest)prevBlendFuncDstAlpha);
        if (prevBlendEnabled)
            GL.Enable(EnableCap.Blend);
        else
            GL.Disable(EnableCap.Blend);

        if (prevDepthTestEnabled)
            GL.Enable(EnableCap.DepthTest);
        else
            GL.Disable(EnableCap.DepthTest);

        if (prevCullFaceEnabled)
            GL.Enable(EnableCap.CullFace);
        else
            GL.Disable(EnableCap.CullFace);

        if (prevScissorTestEnabled)
            GL.Enable(EnableCap.ScissorTest);
        else
            GL.Disable(EnableCap.ScissorTest);


        GL.PolygonMode(MaterialFace.FrontAndBack, (PolygonMode)prevPolygonMode[0]);
    }

    private static void CheckGlError(string title)
    {
        ErrorCode error;
        var i = 1;
        while ((error = GL.GetError()) != ErrorCode.NoError)
        {
            Log.Error($"{title} ({i++}): {error}");
        }
    }

    public void Dispose()
    {
        _vao.Unload();
        _vbo.Unload();
        _ibo.Unload();
        _shader.Unload();

        GL.DeleteTexture(_fontTexture);
    }
}