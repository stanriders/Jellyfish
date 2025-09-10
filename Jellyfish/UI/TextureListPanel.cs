using System;
using System.Linq;
using System.Numerics;
using Hexa.NET.ImGui;
using Jellyfish.Console;
using OpenTK.Graphics.OpenGL;

namespace Jellyfish.UI;

public class EnableTextureList() : ConVar<bool>("edt_texturelist");

public class TextureListPanel : IUiPanel
{
    private const int item_width = 250;
    private int? _expandedTexture;
    private static int _atlasedCubemapHandle;

    public unsafe void Frame(double timeElapsed)
    {
        if (!ConVarStorage.Get<bool>("edt_texturelist"))
            return;

        var textureCount = Engine.TextureManager.Textures.Count;

        if (ImGui.Begin("Texture list"))
        {
            ImGui.Text($"{textureCount} textures");
            for (int i = 0; i < textureCount; i++)
            {
                var texture = Engine.TextureManager.Textures.ElementAt(i);
                ImGui.BeginGroup();
                var expanded = _expandedTexture == i;

                ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + item_width);
                if (expanded)
                    ImGui.Text($"{texture.Params.Name}: {(texture.Params.Srgb ? "[SRGB] " : "")}{texture.References} references, {texture.Levels} levels, {texture.Format}");
                else
                    ImGui.Text($"{texture.Params.Name} ({texture.References} references)");
                ImGui.PopTextWrapPos();

                var size = expanded ? item_width * 2 : item_width;
                bool pressed;
                // flip RTs upside down
                if (texture.Params.RenderTargetParams != null)
                {
                    if (texture.Params.Type == TextureTarget.TextureCubeMap)
                    {
                        if (_atlasedCubemapHandle != 0)
                            GL.DeleteTexture(_atlasedCubemapHandle);

                        _atlasedCubemapHandle = CreateCubemapCross(texture.Handle, 256);

                        pressed = ImGui.ImageButton(texture.Params.Name, new ImTextureRef(texId: _atlasedCubemapHandle),
                            new Vector2(size, size), new Vector2(0, 1),
                            new Vector2(1, 0));
                    }
                    else
                    {
                        pressed = ImGui.ImageButton(texture.Params.Name, new ImTextureRef(texId: texture.Handle),
                            new Vector2(size, size), new Vector2(0, 1),
                            new Vector2(1, 0));
                    }
                }
                else
                    pressed = ImGui.ImageButton(texture.Params.Name, new ImTextureRef(texId: texture.Handle), new Vector2(size, size));
                
                if (pressed)
                {
                    _expandedTexture = i;
                }
                
                ImGui.EndGroup();

                var windowSize = ImGui.GetWindowPos().X + ImGui.GetContentRegionAvail().X;
                var prevGroup = ImGui.GetItemRectMax().X;
                var nextGroup = prevGroup + item_width / 2.0f; // divided by 2 to make ux slightly better
                if (i + 1 < textureCount && nextGroup < windowSize)
                    ImGui.SameLine();
            }
        }
        ImGui.End();
    }

    private int CreateCubemapCross(int cubemapHandle, int faceSize)
    {
        int width = faceSize * 4;
        int height = faceSize * 3;

        int atlasTex = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, atlasTex);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba8, width, height, 0,
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

            GL.CopyTexSubImage2D(TextureTarget.Texture2d, 0,
                xOffset, yOffset,  // destination offset in atlas
                0, 0,              // source from cubemap face
                faceSize, faceSize);
        }

        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
        GL.DeleteFramebuffer(fbo);

        return atlasTex;
    }
}