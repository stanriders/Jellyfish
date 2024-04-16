namespace Jellyfish.Entities;

public class DynamicModel : BaseModelEntity
{
    public string Model
    {
        get => ModelPath;
        set => ModelPath = "models/" + value;
    }
}