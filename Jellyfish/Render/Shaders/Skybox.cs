using Jellyfish.Console;
using Jellyfish.Entities;
using OpenTK.Mathematics;

namespace Jellyfish.Render.Shaders;

public class Skybox : Shader
{
    private Sun? _sun;
    private bool _noSun;

    public Skybox() : base("shaders/Skybox.vert", null, "shaders/Skybox.frag") { }

    public override void Bind()
    {
        if (_sun == null)
        {
            _sun = EntityManager.FindEntity("light_sun") as Sun;
            if (_sun == null && !_noSun)
            {
                Log.Context(this).Error("No sun, sky won't be rendered!");
                _noSun = true;
                return;
            }
        }

        if (_noSun)
            return;

        base.Bind();

        var rotationVector = Vector3.Transform(Vector3.UnitY, _sun!.GetPropertyValue<Quaternion>("Rotation"));
        SetVector3("uSunPos", rotationVector);
        SetFloat("uSunIntensity", _sun.Brightness * 5f);

        SetMatrix4("view", Camera.Instance.GetViewMatrix().ClearTranslation());
        SetMatrix4("projection", Camera.Instance.GetProjectionMatrix());
    }
}