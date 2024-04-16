using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Render.Lighting;

public static class LightManager
{
    private const int max_lights = 4;
    private static readonly List<ILightSource> lights = new(max_lights);

    public static void AddLight(ILightSource light)
    {
        if (lights.Count < max_lights)
            lights.Add(light);
    }

    public static ILightSource[] GetLightSources()
    {
        return lights.Where(x => x.Enabled).ToArray();
    }
}