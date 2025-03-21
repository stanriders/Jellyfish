using System;
using System.Linq;
using ImGuiNET;
using Jellyfish.Console;
using Jellyfish.Entities;
using OpenTK.Mathematics;

namespace Jellyfish.UI;

public class EntityListPanel : IUiPanel
{
    private string? _selectedEntityType;

    public void Frame()
    {
        if (!ConVarStorage.Get<bool>("edt_enable"))
            return;

        if (EntityManager.Entities == null || EntityManager.EntityClasses == null)
            return;

        if (!MainWindow.Loaded)
            return;
        
        if (ImGui.Begin("Entity list"))
        {
            foreach (var entity in EntityManager.Entities)
            {
                var entityName = entity.GetPropertyValue<string>("Name");
                ImGui.PushID(entityName);
                var header = $"{entityName} ({entity.GetType().Name})";
                if (!entity.Loaded)
                {
                    header = $"[UNLOADED] {entity.GetType().Name}";
                }

                if (ImGui.CollapsingHeader(header))
                {
                    foreach (var entityProperty in entity.EntityProperties)
                    {
                        AddProperty(entity, entityProperty);
                    }

                    ImGui.Spacing();

                    foreach (var entityAction in entity.EntityActions.OrderBy(x=> x.Name))
                    {
                       if (ImGui.Button(entityAction.Name))
                           entityAction.Act();
                    }

                    ImGui.Spacing();

                    if (!entity.Loaded)
                    {
                        if (ImGui.Button($"Load"))
                        {
                            entity.Load();
                        }
                    }
                    ImGui.PopID();
                }
            }

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Add entity"))
            {
                ImGui.BeginListBox("Entity types");

                foreach (var entityClass in EntityManager.EntityClasses.Order())
                {
                    if (ImGui.MenuItem(entityClass, "" ,entityClass == _selectedEntityType))
                    {
                        _selectedEntityType = entityClass;
                    }
                }

                ImGui.EndListBox();

                if (_selectedEntityType != null)
                {
                    if (ImGui.Button("Spawn"))
                    {
                        EntityManager.CreateEntity(_selectedEntityType);
                    }
                }
            }

            ImGui.End();
        }
    }

    private void AddProperty(BaseEntity entity, EntityProperty entityProperty)
    {
        if (entity.Loaded)
        {
            if (entityProperty.Name == "Name")
            {
                return;
            }

            if (!entityProperty.Editable)
            {
                ImGui.Text($"{entityProperty.Name}: {entityProperty.Value}");
                return;
            }
        }
        
        var propertyName = entityProperty.Name;
        var elementLabel = propertyName;

        if (entityProperty.Type == typeof(Vector2))
        {
            var valueCasted = (Vector2)entityProperty.Value!;

            var val = new System.Numerics.Vector2(valueCasted.X, valueCasted.Y);
            ImGui.DragFloat2(elementLabel, ref val);

            entity.SetPropertyValue(propertyName, new Vector2(val.X, val.Y));
        }
        else if (entityProperty.Type == typeof(Vector3))
        {
            var valueCasted = (Vector3)entityProperty.Value!;

            var val = valueCasted.ToNumericsVector();
            ImGui.DragFloat3(elementLabel, ref val);

            entity.SetPropertyValue(propertyName, val.ToOpentkVector());
        }
        else if (entityProperty.Type == typeof(Color4<Rgba>))
        {
            var valueCasted = (Color4<Rgba>)entityProperty.Value!;

            var val = new System.Numerics.Vector4(valueCasted.X, valueCasted.Y, valueCasted.Z, valueCasted.W);
            ImGui.DragFloat4(elementLabel, ref val, 0.01f, 0.0f, 1.0f);

            entity.SetPropertyValue(propertyName, new Color4<Rgba>(val.X, val.Y, val.Z, val.W));
        }
        else if (entityProperty.Type == typeof(Color3<Rgb>))
        {
            var valueCasted = (Color3<Rgb>)entityProperty.Value!;

            var val = new System.Numerics.Vector3(valueCasted.X, valueCasted.Y, valueCasted.Z);
            ImGui.DragFloat3(elementLabel, ref val, 0.01f, 0.0f, 1.0f);

            entity.SetPropertyValue(propertyName, new Color3<Rgb>(val.X, val.Y, val.Z));
        }
        else if (entityProperty.Type == typeof(Quaternion))
        {
            var valueCasted = (Quaternion)entityProperty.Value!;
            var eulerAngles = valueCasted.ToEulerAngles();

            var val = new System.Numerics.Vector3(MathHelper.RadiansToDegrees(eulerAngles.X),
                MathHelper.RadiansToDegrees(eulerAngles.Y),
                MathHelper.RadiansToDegrees(eulerAngles.Z));

            ImGui.DragFloat3(elementLabel, ref val, 1f, -360.0f, 360.0f);

            entity.SetPropertyValue(propertyName,
                new Quaternion(MathHelper.DegreesToRadians(val.X), MathHelper.DegreesToRadians(val.Y), MathHelper.DegreesToRadians(val.Z)));
        }
        else if (entityProperty.Type == typeof(bool))
        {
            var val = (bool)entityProperty.Value!;
            ImGui.Checkbox(elementLabel, ref val);
            entity.SetPropertyValue(propertyName, val);
        }
        else if (entityProperty.Type == typeof(int))
        {
            var val = (int)entityProperty.Value!;
            ImGui.DragInt(elementLabel, ref val);
            entity.SetPropertyValue(propertyName, val);
        }
        else if (entityProperty.Type == typeof(float))
        {
            var val = (float)entityProperty.Value!;
            var speed = val > 1.0f ? 1.0f : 0.01f;
            ImGui.DragFloat(elementLabel, ref val, speed);
            entity.SetPropertyValue(propertyName, val);
        }
        else if (entityProperty.Type == typeof(string))
        {
            var val = (string?)entityProperty.Value ?? string.Empty;
            ImGui.InputText(elementLabel, ref val, 1024);
            entity.SetPropertyValue(propertyName, val);
        }
        else if (entityProperty.Type == typeof(Enum))
        {
            var val = (int)entityProperty.Value!;
            ImGui.DragInt(elementLabel, ref val);
            entity.SetPropertyValue(propertyName, val);
        }
        else
        {
            ImGui.Text($"{entityProperty.Name}: {entityProperty.Value}");
        }
    }
}