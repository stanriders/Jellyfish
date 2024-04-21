using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Render.Lighting;

public static class LightManager
{
    private const int max_lights = 4;
    private static readonly List<ILightSource> Lights = new(max_lights);

    public static void AddLight(ILightSource light)
    {
        if (Lights.Count < max_lights)
            Lights.Add(light);
    }

    public static ILightSource[] GetLightSources()
    {
        return Lights.Where(x => x.Enabled).ToArray();
    }
}