using Hexa.NET.ImGui;
using Jellyfish.Console;
using Jellyfish.Render;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Jellyfish.UI;

public class EnableTextureList() : ConVar<bool>("edt_texturelist", defaultBind: Keys.T);

public class TextureListPanel : IUiPanel
{
    private enum Tabs
    {
        All,
        Engine,
        RTs,
        Textures
    }

    private const int item_width = 200;
    private Texture? _expandedTexture;
    private readonly Dictionary<string, int> _cubemapAtlases = new();
    private Tabs _currentTab = Tabs.All;
    private readonly Texture _cubeTexture = Engine.TextureManager.GetTexture(new TextureParams
    {
        Srgb = false, 
        Name = "_engine_Cube",
        Path = "materials/engine/cube.png",
        MaxLevels = 1
    }).Texture;

    public unsafe void Frame(double timeElapsed)
    {
        if (!ConVarStorage.Get<bool>("edt_texturelist"))
            return;

        var textureCount = Engine.TextureManager.Textures.Count;

        if (ImGui.Begin("Texture list"))
        {
            ImGui.Text($"{textureCount} textures");

            if (ImGui.BeginChild("Textures", ImGuiChildFlags.ResizeX))
            {
                if (ImGui.BeginTabBar("Tabs"))
                {
                    if (ImGui.TabItemButton("All", _currentTab == Tabs.All ? ImGuiTabItemFlags.UnsavedDocument : ImGuiTabItemFlags.None))
                        _currentTab = Tabs.All;

                    if (ImGui.TabItemButton("Engine Textures", _currentTab == Tabs.Engine ? ImGuiTabItemFlags.UnsavedDocument : ImGuiTabItemFlags.None))
                        _currentTab = Tabs.Engine;

                    if (ImGui.TabItemButton("RTs", _currentTab == Tabs.RTs ? ImGuiTabItemFlags.UnsavedDocument : ImGuiTabItemFlags.None))
                        _currentTab = Tabs.RTs;

                    if (ImGui.TabItemButton("World Textures", _currentTab == Tabs.Textures ? ImGuiTabItemFlags.UnsavedDocument : ImGuiTabItemFlags.None))
                        _currentTab = Tabs.Textures;
                }
                ImGui.EndTabBar();

                for (int i = 0; i < textureCount; i++)
                {
                    var texture = Engine.TextureManager.Textures.ElementAt(i);

                    if (_currentTab == Tabs.Engine && !texture.Params.Name!.StartsWith("_"))
                        continue;

                    if (texture.RenderTargetParams != null && _currentTab == Tabs.Textures)
                        continue;

                    if (texture.RenderTargetParams == null && _currentTab == Tabs.RTs)
                        continue;

                    ImGui.BeginGroup();
                    var size = item_width;

                    ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + size);
                    ImGui.Text($"{texture.Params.Name} ({texture.References} references)");
                    ImGui.PopTextWrapPos();

                    bool pressed;
                    // flip RTs upside down
                    if (texture.RenderTargetParams != null)
                    {
                        if (texture.Params.Type == TextureTarget.TextureCubeMap)
                        {
                            pressed = ImGui.ImageButton(texture.Params.Name, new ImTextureRef(texId: _cubeTexture.Handle), new Vector2(size, size));
                        }
                        else
                        {
                            pressed = ImGui.ImageButton(texture.Params.Name, new ImTextureRef(texId: texture.Handle),
                                new Vector2(size, size), new Vector2(0, 1), new Vector2(1, 0));
                        }
                    }
                    else
                        pressed = ImGui.ImageButton(texture.Params.Name, new ImTextureRef(texId: texture.Handle), new Vector2(size, size));

                    if (pressed)
                    {
                        _expandedTexture = texture;
                    }

                    ImGui.EndGroup();

                    var windowSize = ImGui.GetWindowPos().X + ImGui.GetContentRegionAvail().X;
                    var prevGroup = ImGui.GetItemRectMax().X;
                    var nextGroup = prevGroup + item_width / 2.0f; // divided by 2 to make ux slightly better
                    if (i + 1 < textureCount && nextGroup < windowSize)
                        ImGui.SameLine();
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();

            if (ImGui.BeginChild("Expanded"))
            {
                var windowWidth = ImGui.GetContentRegionAvail().X;
                var maxSize = Math.Max(item_width * 2, windowWidth);
                var bgColor = new Vector4(0, 0, 0, 1);

                if (_expandedTexture != null)
                {
                    var size = new Vector2(maxSize);

                    var aspect = _expandedTexture.Size.X / _expandedTexture.Size.Y;
                    var widthScale = _expandedTexture.Size.X / maxSize;
                    var heightScale = _expandedTexture.Size.Y / maxSize;

                    if (widthScale > heightScale)
                    {
                        size.Y /= aspect;
                    }
                    else if (widthScale < heightScale)
                    {
                        size.X *= aspect;
                    }

                    ImGui.PushFont(null, 20f);
                    ImGui.Text($"{(_expandedTexture.Params.Srgb ? "[SRGB] " : "")}{_expandedTexture.Params.Name} ({_expandedTexture.References} references)");
                    ImGui.PopFont();
                    if (_expandedTexture.Params.Path != null && _expandedTexture.Params.Path != _expandedTexture.Params.Name)
                        ImGui.Text(_expandedTexture.Params.Path);
                    ImGui.Separator();
                    ImGui.Text($"Size: {_expandedTexture.Size.X}x{_expandedTexture.Size.Y}\tLevels: {_expandedTexture.Levels}\tFormat: {_expandedTexture.Format}");
                    ImGui.Text($"Type: {_expandedTexture.Params.Type}\tMin filter: {_expandedTexture.Params.MinFiltering}\tMag filter: {_expandedTexture.Params.MagFiltering}");
                    ImGui.Text($"Wrap mode: {_expandedTexture.Params.WrapMode}\tBorder color: [{string.Join(';', _expandedTexture.Params.BorderColor ?? [])}]");

                    if (_expandedTexture.RenderTargetParams != null)
                    {
                        ImGui.Text($"Attachment: {_expandedTexture.RenderTargetParams.Attachment}");

                        if (_expandedTexture.Params.Type == TextureTarget.TextureCubeMap)
                        {
                            var name = _expandedTexture.Params.Name!;
                            _cubemapAtlases.TryAdd(name, 0);

                            if (_cubemapAtlases[name] != 0)
                                GL.DeleteTexture(_cubemapAtlases[name]);

                            _cubemapAtlases[name] = CreateCubemapCross(_expandedTexture.Handle, _expandedTexture.RenderTargetParams.Width);

                            ImGui.ImageWithBg(new ImTextureRef(texId: _cubemapAtlases[name]),
                                size, new Vector2(0, 1), new Vector2(1, 0), bgColor); // flip RTs upside down
                        }
                        else
                        {
                            ImGui.ImageWithBg(new ImTextureRef(texId: _expandedTexture.Handle),
                                size, new Vector2(0, 1), new Vector2(1, 0), bgColor);
                        }
                    }
                    else
                    {
                        ImGui.Text($"Has alpha: {_expandedTexture.HasAlpha}");
                        ImGui.ImageWithBg(new ImTextureRef(texId: _expandedTexture.Handle), size, bgColor);
                    }
                }
            }
            ImGui.EndChild();
        }
        ImGui.End();
    }

    public void Unload()
    {
        foreach (var cubemapAtlas in _cubemapAtlases)
        {
            GL.DeleteTexture(cubemapAtlas.Value);
        }

        Engine.TextureManager.RemoveTexture(_cubeTexture);
    }

    private int CreateCubemapCross(int cubemapHandle, int faceSize)
    {
        int width = faceSize * 4;
        int height = faceSize * 3;

        int atlasTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, atlasTex);
        GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TextureParameteri(atlasTex, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TextureParameteri(atlasTex, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        int fbo = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fbo);

        // Define placement of each face
        var placements = new (TextureTarget Face, int X, int Y)[]
        {
            (TextureTarget.TextureCubeMapPositiveX, 2, 1), // +X
            (TextureTarget.TextureCubeMapNegativeX, 0, 1), // -X
            (TextureTarget.TextureCubeMapPositiveY, 1, 0), // +Y
            (TextureTarget.TextureCubeMapNegativeY, 1, 2), // -Y
            (TextureTarget.TextureCubeMapPositiveZ, 1, 1), // +Z
            (TextureTarget.TextureCubeMapNegativeZ, 3, 1), // -Z
        };

        foreach (var (face, gridX, gridY) in placements)
        {
            GL.FramebufferTexture2D(FramebufferTarget.ReadFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                face,
                cubemapHandle, 0);

            int xOffset = gridX * faceSize;
            int yOffset = gridY * faceSize;

            GL.CopyTexSubImage2D(TextureTarget.Texture2D, 0,
                xOffset, yOffset,  // destination offset in atlas
                0, 0,              // source from cubemap face
                faceSize, faceSize);
        }

        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        GL.DeleteFramebuffer(fbo);

        return atlasTex;
    }
}