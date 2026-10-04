using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hexa.NET.ImGui;
using Jellyfish.Console;
using Jellyfish.Entities;
using Jellyfish.Render;
using Newtonsoft.Json;
using Vector2 = System.Numerics.Vector2;

namespace Jellyfish.UI;

public class EnableMaterialBrowser() : ConVar<bool>("edt_materialbrowser", true);

public class MaterialBrowser : IUiPanel
{
    // material path relative to the materials folder, used by the editor for new blocks and when applying materials to faces
    public static string SelectedMaterial { get; set; } = string.Empty;

    private const string materials_directory = "materials";
    private const float preview_size = 128.0f;

    private string[]? _materials;
    private string _filter = string.Empty;

    private string? _previewMaterial;
    private Texture? _previewTexture;

    public unsafe void Frame(double timeElapsed)
    {
        if (!ConVarStorage.Get<bool>("edt_enable") || !ConVarStorage.Get<bool>("edt_materialbrowser"))
            return;

        _materials ??= FindMaterials();

        if (string.IsNullOrEmpty(SelectedMaterial))
            SelectedMaterial = _materials[0];

        if (_previewMaterial != SelectedMaterial)
            LoadPreview();

        ImGui.SetNextWindowBgAlpha(0.5f);
        if (ImGui.Begin("Materials"))
        {
            if (_previewTexture != null)
            {
                ImGui.Image(new ImTextureRef(texId: _previewTexture.Handle), new Vector2(preview_size));
                ImGui.SameLine();
            }

            ImGui.TextWrapped(SelectedMaterial);

            ImGui.InputTextWithHint("##Filter", "Filter", ref _filter, 256);
            ImGui.SameLine();
            if (ImGui.Button("Refresh"))
                _materials = FindMaterials();

            if (ImGui.BeginListBox("##Materials", new Vector2(-1, -1)))
            {
                foreach (var material in _materials.Where(x => x.Contains(_filter, StringComparison.OrdinalIgnoreCase)))
                {
                    if (ImGui.Selectable(material, material == SelectedMaterial))
                        SelectedMaterial = material;
                }

                ImGui.EndListBox();
            }
        }

        ImGui.End();
    }

    public void Unload()
    {
        if (_previewTexture != null)
            Engine.TextureManager.RemoveTexture(_previewTexture);

        _previewTexture = null;
    }

    private static string[] FindMaterials()
    {
        if (!Directory.Exists(materials_directory))
            return [];

        return Directory.EnumerateFiles(materials_directory, "*.mat", SearchOption.AllDirectories)
            .Select(x => Path.GetRelativePath(materials_directory, x).Replace('\\', '/'))
            .Where(x => !x.StartsWith("models/") && !x.StartsWith("engine/"))
            .Order()
            .ToArray();
    }

    private void LoadPreview()
    {
        Unload();
        _previewMaterial = SelectedMaterial;

        var path = Path.Combine(materials_directory, SelectedMaterial);
        if (!File.Exists(path))
            return;

        var materialParams = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path),
            new JsonSerializerSettings { Error = (_, _) => { } });

        if (materialParams?.GetValueOrDefault("Diffuse") is not string diffuse)
            return;

        // same path format as the material shaders use so the texture is shared if it's already loaded
        _previewTexture = Engine.TextureManager.GetTexture(new TextureParams
        {
            Path = $"{Path.GetDirectoryName(path)}/{diffuse}",
            Srgb = true
        }).Texture;
    }
}
