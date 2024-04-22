using System.Linq;
using ImGuiNET;
using Jellyfish.Entities;

namespace Jellyfish.UI;

public class EntityListPanel : IUiPanel
{
    public void Frame()
    {
        if (EntityManager.Entities == null)
            return;
        
        if (ImGui.Begin("Entity list"))
        {
            foreach (var entity in EntityManager.Entities)
            {
                if (ImGui.CollapsingHeader($"{entity.GetPropertyValue<string>("Name")} ({entity.GetType().Name})"))
                {
                    foreach (var entityProperty in entity.EntityProperties.Where(x=> x.Name != "Name"))
                    {
                        ImGui.Text($"{entityProperty.Name}: {entityProperty.Value}");
                    }
                    ImGui.Spacing();
                }
            }

            ImGui.End();
        }
    }
}