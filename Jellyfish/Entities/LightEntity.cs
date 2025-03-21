using Jellyfish.Render.Lighting;
using OpenTK.Mathematics;

namespace Jellyfish.Entities
{
    public abstract class LightEntity : BaseEntity, ILightSource
    {
        protected LightEntity()
        {
            AddProperty("Color", new Color3<Rgb>(1, 1, 1));
            AddProperty("Ambient", new Color3<Rgb>(0.1f, 0.1f, 0.1f));
            AddProperty("Brightness", 1f);
            AddProperty("Enabled", true);
            AddProperty("Shadows", true);
        }

        public override void Load()
        {
            base.Load();
            LightManager.AddLight(this);
        }

        public override void Unload()
        {
            LightManager.RemoveLight(this);
            base.Unload();
        }

        public Vector3 Position => GetPropertyValue<Vector3>("Position");
        public Quaternion Rotation => GetPropertyValue<Quaternion>("Rotation");
        public Color3<Rgb> Color => GetPropertyValue<Color3<Rgb>>("Color");
        public Color3<Rgb> Ambient => GetPropertyValue<Color3<Rgb>>("Ambient");
        public float Brightness => GetPropertyValue<float>("Brightness");
        public bool Enabled => GetPropertyValue<bool>("Enabled");
        public bool UseShadows => GetPropertyValue<bool>("Shadows");
        public abstract float NearPlane { get; }
        public abstract float FarPlane { get; }
        public abstract Matrix4[] Projections { get; }
        public abstract int ShadowResolution { get; }
    }
}
