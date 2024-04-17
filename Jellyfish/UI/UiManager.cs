using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Jellyfish.UI;

public static class UiManager
{
    private static readonly List<IUiPanel> Panels = new();

    public static void Load()
    {
        var panels = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsPublic: true, IsAbstract: false } && typeof(IUiPanel).IsAssignableFrom(x));

        foreach (var panelType in panels)
        {
            if (Activator.CreateInstance(panelType) is IUiPanel panel)
            {
                Panels.Add(panel);
            }
            else
            {
                Log.Error("Can't create panel {Type}", panelType.Name);
            }
        }
    }

    public static void Frame()
    {
        foreach (var panel in Panels)
        {
            panel.Frame();
        }
    }
}