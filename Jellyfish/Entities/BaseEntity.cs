using OpenTK.Mathematics;

namespace Jellyfish.Entities;

public abstract class BaseEntity
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }

    public virtual void Load()
    {
    }

    public virtual void Unload()
    {
    }

    public virtual void Think()
    {
    }
}