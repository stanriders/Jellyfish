using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jellyfish.Console;

namespace Jellyfish.UI;

public class UiManager
{
    private readonly List<IUiPanel> _panels = new();

    public UiManager()
    {
        Load();
    }

    public void Load()
    {
        var panels = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(x => x is { IsPublic: true, IsAbstract: false } && typeof(IUiPanel).IsAssignableFrom(x));

        foreach (var panelType in panels)
        {
            if (Activator.CreateInstance(panelType) is IUiPanel panel)
            {
                _panels.Add(panel);
            }
            else
            {
                Log.Context(this).Error("Can't create panel {Type}", panelType.Name);
            }
        }
    }

    public void Frame()
    {
        foreach (var panel in _panels)
        {
            panel.Frame();
        }
    }
}