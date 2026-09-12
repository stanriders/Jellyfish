using Hexa.NET.ImGui;
using Jellyfish.Console;

namespace Jellyfish.UI.Components;

public static class ConVarComponents
{
    public static void Checkbox(string convar, string name)
    {
        var val = ConVarStorage.Get<bool>(convar);
        ImGui.Checkbox(name, ref val);
        ConVarStorage.Set(convar, val);
    }
    
    public static void MenuItem(string convar, string name, string? shortcut = null)
    {
        var val = ConVarStorage.Get<bool>(convar);
        if (ImGui.MenuItem(name, shortcut ?? string.Empty, ref val))
        {
            ConVarStorage.Set(convar, val);
        }
    }
}