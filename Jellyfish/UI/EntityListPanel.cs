using System.Linq;
using ImGuiNET;
using Jellyfish.Entities;
using OpenTK.Mathematics;

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
                        if (entityProperty.Type == typeof(Vector2))
                        {
                            var valueCasted = (Vector2)entityProperty.Value!;

                            var val = new System.Numerics.Vector2(valueCasted.X, valueCasted.Y);
                            ImGui.DragFloat2($"{entityProperty.Name}", ref val);

                            entity.SetPropertyValue(entityProperty.Name, new Vector2(val.X, val.Y));
                        }
                        else if (entityProperty.Type == typeof(Vector3))
                        {
                            var valueCasted = (Vector3)entityProperty.Value!;

                            var val = valueCasted.ToNumericsVector();
                            ImGui.DragFloat3($"{entityProperty.Name}", ref val);

                            entity.SetPropertyValue(entityProperty.Name, val.ToOpentkVector());
                        }
                        else if (entityProperty.Type == typeof(Color4))
                        {
                            var valueCasted = (Color4)entityProperty.Value!;

                            var val = new System.Numerics.Vector4(valueCasted.R, valueCasted.G, valueCasted.B, valueCasted.A);
                            ImGui.DragFloat4($"{entityProperty.Name}", ref val, 0.01f, 0.0f, 1.0f);

                            entity.SetPropertyValue(entityProperty.Name, new Color4(val.X, val.Y, val.Z, val.W));
                        }
                        else if (entityProperty.Type == typeof(bool))
                        {
                            var val = (bool)entityProperty.Value!;
                            ImGui.Checkbox(entityProperty.Name, ref val);
                            entity.SetPropertyValue(entityProperty.Name, val);
                        }
                        else if (entityProperty.Type == typeof(int))
                        {
                            var val = (int)entityProperty.Value!;
                            ImGui.DragInt(entityProperty.Name, ref val);
                            entity.SetPropertyValue(entityProperty.Name, val);
                        }
                        else if (entityProperty.Type == typeof(float))
                        {
                            var val = (float)entityProperty.Value!;
                            var speed = val > 1.0f ? 1.0f : 0.01f;
                            ImGui.DragFloat(entityProperty.Name, ref val, speed);
                            entity.SetPropertyValue(entityProperty.Name, val);
                        }
                        else
                        {
                            ImGui.Text($"{entityProperty.Name}: {entityProperty.Value}");
                        }
                    }
                    ImGui.Spacing();
                }
            }

            ImGui.End();
        }
    }
}