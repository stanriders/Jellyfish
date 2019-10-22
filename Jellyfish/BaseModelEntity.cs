
using Jellyfish.Render;

namespace Jellyfish
{
    public abstract class BaseModelEntity : BaseEntity
    {
        private Model model;

        protected string ModelPath { get; set; }

        public override void Load()
        {
            if (!string.IsNullOrEmpty(ModelPath))
                model = new Model(ModelPath);

            base.Load();
        }

        public override void Think()
        {
            if (model != null)
            {
                model.Position = Position;
                model.Rotation = Rotation;
            }

            base.Think();
        }
    }
}
