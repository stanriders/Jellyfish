
using System.Collections.Generic;
using System.Linq;

namespace Jellyfish.Render.Lighting
{
    public static class LightManager
    {
        private static List<ILightSource> lights = new List<ILightSource>();

        public static void AddLight(ILightSource light)
        {
            lights.Add(light);
        }

        public static ILightSource[] GetLightSources()
        {
            return lights.Where(x => x.Enabled).ToArray();
        }
    }
}
