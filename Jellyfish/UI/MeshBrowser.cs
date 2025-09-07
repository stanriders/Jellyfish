using Hexa.NET.ImGui;
using Jellyfish.Console;
using Jellyfish.Render;
using System;
using System.Numerics;

namespace Jellyfish.UI;

public class EnableMeshBrowser() : ConVar<bool>("edt_meshbrowser");
public class MeshBrowser : IUiPanel
{
    private Mesh? _selectedMesh;

    public void Frame(double timeElapsed)
    {
        if (!ConVarStorage.Get<bool>("edt_meshbrowser"))
            return;

        if (ImGui.Begin("Meshes"))
        {
            if (ImGui.BeginListBox("##Entity list", new Vector2(-1, 10 * ImGui.GetTextLineHeightWithSpacing())))
            {
                if (ImGui.MenuItem("-- Deselect --"))
                {
                    _selectedMesh = null;
                }

                foreach (var mesh in Engine.MeshManager.Meshes)
                {
                    ImGui.PushID(mesh.Name+Random.Shared.Next());
                    if (ImGui.MenuItem(mesh.Name, "", _selectedMesh?.Name == mesh.Name))
                    {
                        _selectedMesh = mesh;
                    }

                    ImGui.PopID();
                }

                ImGui.EndListBox();
            }

            ImGui.Separator();

            if (_selectedMesh != null)
            {
                ImGui.Text($"Material: {_selectedMesh.Material?.Name}");
                ImGui.Text($"Model: {_selectedMesh.Model?.Name}");
                ImGui.Text($"Vertices: {_selectedMesh.Vertices.Count}");
                ImGui.Text($"Bones: {_selectedMesh.Model?.Bones.Count ?? 0}");
                if (_selectedMesh.Model?.Bones is { Count: > 0 })
                {
                    foreach (var bone in _selectedMesh.Model.Bones)
                    {
                        ImGui.Text($"\t{bone.Id}. {bone.Name}");
                    }
                }
                ImGui.Text($"Animations: {_selectedMesh.Model?.Animations.Count ?? 0}");
                if (_selectedMesh.Model?.Animations is { Count: > 0 })
                {
                    foreach (var animation in _selectedMesh.Model.Animations)
                    {
                        ImGui.Text($"\t- {animation.Name} ({animation.Duration})");
                    }
                }
                
                DebugRender.DrawBoundingBox(_selectedMesh.Position, _selectedMesh.BoundingBox);
            }
        }

        ImGui.End();
    }
}