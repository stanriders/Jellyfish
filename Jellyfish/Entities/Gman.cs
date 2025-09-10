using OpenTK.Mathematics;

namespace Jellyfish.Entities;

[Entity("npc_gman")]
public class Gman : BaseModelEntity
{
    public Gman()
    {
        ModelPath = "models/Gman_high_reference.smd";
        SetPropertyValue("Animation", "idle3");
    }
}