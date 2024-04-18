using ImGuiNET;
using Jellyfish.Entities;

namespace Jellyfish.UI;

public class EntityListPanel : IUiPanel
{
    public void Frame()
    {
        if (EntityManager.Entities == null)
            return;

        ImGui.Begin("Entity list");
        ImGui.BeginTable("entitiesTable", 2);
        ImGui.TableSetupColumn("Class name");
        ImGui.TableSetupColumn("Position");
        ImGui.TableHeadersRow();

        foreach (var entity in EntityManager.Entities)
        {
            ImGui.TableNextRow(ImGuiTableRowFlags.None);

            ImGui.TableNextColumn();
            ImGui.Text(entity.GetType().Name);

            ImGui.TableNextColumn();
            ImGui.Text(entity.Position.ToString());
        }

        ImGui.EndTable();
        ImGui.End();
    }
}