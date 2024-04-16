using OpenTK.Mathematics;

namespace Jellyfish;

public abstract class BaseEntity
{
    /*
    private static string className;
    public static string ClassName
    {
        get => className;
        set 
        { 
            className = value; 
            EntityManager.AddClassName(className, typeof(BaseEntity));
        }
    }
    */

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