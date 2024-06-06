using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("npc_gman")]
public class Gman : BaseModelEntity
{
    public Gman()
    {
        ModelPath = "models/Gman_high_reference.smd";
    }
    public override void Think()
    {
        if (Model != null)
        {
            Model.Position = GetPropertyValue<Vector3>("Position");
            Model.Rotation = GetPropertyValue<Quaternion>("Rotation");
        }

        base.Think();
    }
}