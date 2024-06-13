﻿using ImGuiNET;
using System;
using OpenTK.Graphics.OpenGL;
using Jellyfish.Render.Buffers;
using System.Runtime.CompilerServices;
using Jellyfish.Render.Shaders;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Serilog;
using ErrorCode = OpenTK.Graphics.OpenGL.ErrorCode;
using Jellyfish.Input;
using System.Collections.Generic;
using System.Numerics;
using ImGuizmoNET;

namespace Jellyfish.Render;

public sealed class ImguiController : IDisposable, IInputHandler
{
    private bool _frameBegun;
    private int _fontTexture;

    private Imgui _shader = null!;
    private VertexArray _vao = null!;
    private VertexBuffer _vbo = null!;
    private IndexBuffer _ibo = null!;

    private int _windowWidth;
    private int _windowHeight;

    private readonly List<char> _pressedChars = new();
    private bool _usingGizmo;

    public ImguiController()
    {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        ImGuizmo.SetImGuiContext(context);

        ImGui.StyleColorsClassic();
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 4f);
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new System.Numerics.Vector4(0.08f, 0.08f, 0.08f, 0.93f));
        ImGui.PushStyleColor(ImGuiCol.TitleBg, new System.Numerics.Vector4(0.18f, 0.18f, 0.18f, 0.83f));
        ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new System.Numerics.Vector4(0.25f, 0.25f, 0.25f, 0.87f));
        ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, new System.Numerics.Vector4(0.04f, 0.04f, 0.04f, 0.20f));
        ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.20f));

        var io = ImGui.GetIO();
        io.Fonts.AddFontFromFileTTF("fonts/Roboto-Regular.ttf", 15f);
        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();

        InputManager.RegisterInputHandler(this);

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void CreateDeviceResources()
    {
        _vbo = new VertexBuffer(usage: BufferUsageHint.DynamicDraw)
        {
            Stride = Unsafe.SizeOf<ImDrawVert>()
        };

        _ibo = new IndexBuffer(usage: BufferUsageHint.DynamicDraw);
        _vao = new VertexArray(_vbo, _ibo);
        RecreateFontDeviceTexture();

        _shader = new Imgui();

        GL.EnableVertexArrayAttrib(_vao.Handle, 0);
        GL.VertexArrayAttribFormat(_vao.Handle, 0, 2, VertexAttribType.Float, false, 0);

        GL.EnableVertexArrayAttrib(_vao.Handle, 1);
        GL.VertexArrayAttribFormat(_vao.Handle, 1, 2, VertexAttribType.Float, false, 8);

        GL.EnableVertexArrayAttrib(_vao.Handle, 2);
        GL.VertexArrayAttribFormat(_vao.Handle, 2, 4, VertexAttribType.UnsignedByte, true, 16);

        GL.VertexArrayAttribBinding(_vao.Handle, 0, 0);
        GL.VertexArrayAttribBinding(_vao.Handle, 1, 0);
        GL.VertexArrayAttribBinding(_vao.Handle, 2, 0);

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

        (var fontTexture, var alreadyExists) = TextureManager.GetTexture("_imgui_Fonts", TextureTarget.Texture2D);
        _fontTexture = fontTexture.Handle;

        if (!alreadyExists)
        {
            GL.TextureStorage2D(_fontTexture, mips, SizedInternalFormat.Rgba8, width, height);

            GL.TextureSubImage2D(_fontTexture, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte,
                pixels);

            GL.GenerateTextureMipmap(_fontTexture);

            GL.TextureParameter(_fontTexture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TextureParameter(_fontTexture, TextureParameterName.TextureMaxLevel, mips - 1);

            GL.TextureParameter(_fontTexture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TextureParameter(_fontTexture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        }

        io.Fonts.SetTexID(_fontTexture);

        io.Fonts.ClearTexData();
    }

    public void Update(int windowWidth, int windowHeigth)
    {
        _windowWidth = windowWidth;
        _windowHeight = windowHeigth;

        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(_windowWidth, _windowHeight);
        io.DeltaTime = (float)MainWindow.Frametime;

        if (_frameBegun)
        {
            ImGui.Render();
        }

        ImGui.NewFrame();
        ImGuizmo.BeginFrame();
        ImGuizmo.SetRect(0, 0, io.DisplaySize.X, io.DisplaySize.Y);
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

        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = new ImVector<ImDrawListPtr>(drawData.CmdListsCount, drawData.CmdListsCount, drawData.CmdLists)[i];

            var vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vbo.Size)
            {
                var newSize = (int)Math.Max(_vbo.Size * 1.5f, vertexSize);
                _vbo.Size = newSize;
                Log.Information($"[ImGui] Resized dear imgui vertex buffer to new size {_vbo.Size}");
            }

            var indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _ibo.Size)
            {
                var newSize = (int)Math.Max(_ibo.Size * 1.5f, indexSize);
                _ibo.Size = newSize;
                Log.Information($"[ImGui] Resized dear imgui index buffer to new size {_ibo.Size}");
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

        _shader.Bind();
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
            var cmdList = new ImVector<ImDrawListPtr>(drawData.CmdListsCount, drawData.CmdListsCount, drawData.CmdLists)[n];//drawData.CmdLists[n];
            ImGuizmo.SetDrawlist(cmdList);

            GL.NamedBufferSubData(_vbo.Handle, nint.Zero,
                cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmdList.VtxBuffer.Data);
            CheckGlError($"Data Vert {n}");

            GL.NamedBufferSubData(_ibo.Handle, nint.Zero, cmdList.IdxBuffer.Size * sizeof(ushort),
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
        GL.UseProgram(0);
        GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
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
            Log.Error($"[ImGui] {title} ({i++}): {error}");
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

    public bool HandleInput(KeyboardState keyboardState, MouseState mouseState, float frameTime)
    {
        var io = ImGui.GetIO();

        io.AddMousePosEvent(mouseState.X, mouseState.Y);
        io.AddMouseButtonEvent(0, mouseState[MouseButton.Left]);
        io.AddMouseButtonEvent(1, mouseState[MouseButton.Right]);
        io.AddMouseButtonEvent(2, mouseState[MouseButton.Middle]);
        
        foreach (Keys key in Enum.GetValues(typeof(Keys)))
        {
            if (key == Keys.Unknown)
            {
                continue;
            }
            io.AddKeyEvent(TranslateKeyToImgui(key), keyboardState.IsKeyDown(key));
        }

        foreach (var c in _pressedChars)
        {
            io.AddInputCharacter(c);
        }
        _pressedChars.Clear();

        io.KeyCtrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
        io.KeyShift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        io.KeySuper = keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);

        if (ImGuizmo.IsUsing())
        {
            _usingGizmo = true;
            InputManager.CaptureInput(this);
            return true;
        }

        if (_usingGizmo && !ImGuizmo.IsUsing())
        {
            _usingGizmo = false;
            InputManager.ReleaseInput(this);
        }

        return io.WantCaptureMouse || io.WantCaptureKeyboard || io.WantTextInput || ImGuizmo.IsOver();
    }

    public void PressChar(char keyChar)
    {
        _pressedChars.Add(keyChar);
    }

    private static ImGuiKey TranslateKeyToImgui(Keys key)
    {
        if (key >= Keys.D0 && key <= Keys.D9)
            return key - Keys.D0 + ImGuiKey._0;

        if (key >= Keys.A && key <= Keys.Z)
            return key - Keys.A + ImGuiKey.A;

        if (key >= Keys.KeyPad0 && key <= Keys.KeyPad9)
            return key - Keys.KeyPad0 + ImGuiKey.Keypad0;

        if (key >= Keys.F1 && key <= Keys.F12)
            return key - Keys.F1 + ImGuiKey.F12;

        switch (key)
        {
            case Keys.Tab: return ImGuiKey.Tab;
            case Keys.Left: return ImGuiKey.LeftArrow;
            case Keys.Right: return ImGuiKey.RightArrow;
            case Keys.Up: return ImGuiKey.UpArrow;
            case Keys.Down: return ImGuiKey.DownArrow;
            case Keys.PageUp: return ImGuiKey.PageUp;
            case Keys.PageDown: return ImGuiKey.PageDown;
            case Keys.Home: return ImGuiKey.Home;
            case Keys.End: return ImGuiKey.End;
            case Keys.Insert: return ImGuiKey.Insert;
            case Keys.Delete: return ImGuiKey.Delete;
            case Keys.Backspace: return ImGuiKey.Backspace;
            case Keys.Space: return ImGuiKey.Space;
            case Keys.Enter: return ImGuiKey.Enter;
            case Keys.Escape: return ImGuiKey.Escape;
            case Keys.Apostrophe: return ImGuiKey.Apostrophe;
            case Keys.Comma: return ImGuiKey.Comma;
            case Keys.Minus: return ImGuiKey.Minus;
            case Keys.Period: return ImGuiKey.Period;
            case Keys.Slash: return ImGuiKey.Slash;
            case Keys.Semicolon: return ImGuiKey.Semicolon;
            case Keys.Equal: return ImGuiKey.Equal;
            case Keys.LeftBracket: return ImGuiKey.LeftBracket;
            case Keys.Backslash: return ImGuiKey.Backslash;
            case Keys.RightBracket: return ImGuiKey.RightBracket;
            case Keys.GraveAccent: return ImGuiKey.GraveAccent;
            case Keys.CapsLock: return ImGuiKey.CapsLock;
            case Keys.ScrollLock: return ImGuiKey.ScrollLock;
            case Keys.NumLock: return ImGuiKey.NumLock;
            case Keys.PrintScreen: return ImGuiKey.PrintScreen;
            case Keys.Pause: return ImGuiKey.Pause;
            case Keys.KeyPadDecimal: return ImGuiKey.KeypadDecimal;
            case Keys.KeyPadDivide: return ImGuiKey.KeypadDivide;
            case Keys.KeyPadMultiply: return ImGuiKey.KeypadMultiply;
            case Keys.KeyPadSubtract: return ImGuiKey.KeypadSubtract;
            case Keys.KeyPadAdd: return ImGuiKey.KeypadAdd;
            case Keys.KeyPadEnter: return ImGuiKey.KeypadEnter;
            case Keys.KeyPadEqual: return ImGuiKey.KeypadEqual;
            case Keys.LeftShift: return ImGuiKey.LeftShift;
            case Keys.LeftControl: return ImGuiKey.LeftCtrl;
            case Keys.LeftAlt: return ImGuiKey.LeftAlt;
            case Keys.LeftSuper: return ImGuiKey.LeftSuper;
            case Keys.RightShift: return ImGuiKey.RightShift;
            case Keys.RightControl: return ImGuiKey.RightCtrl;
            case Keys.RightAlt: return ImGuiKey.RightAlt;
            case Keys.RightSuper: return ImGuiKey.RightSuper;
            case Keys.Menu: return ImGuiKey.Menu;
            default: return ImGuiKey.None;
        }
    }
}