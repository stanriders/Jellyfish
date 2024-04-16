namespace Jellyfish.Entities;

[Entity("model_dynamic")]
public class DynamicModel : BaseModelEntity
{
    public string Model
    {
        get => ModelPath;
        set => ModelPath = "models/" + value;
    }
}