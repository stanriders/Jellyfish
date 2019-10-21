
namespace Jellyfish.Entities
{
    class DynamicModel : BaseModelEntity
    {
        public string Model {
            get => ModelPath;
            set => ModelPath = "models/" + value;
        }
    }
}
