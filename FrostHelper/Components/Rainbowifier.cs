using Monocle;
using System.Linq;

namespace FrostHelper
{
    public class Rainbowifier : Component
    {
        public Rainbowifier() : base(false, true) { }

        public override void Render()
        {
            base.Render();
            foreach (var item in Entity.Components.OfType<Image>())
            {
                item.Color = ColorHelper.GetHue(Scene, item.RenderPosition);
            }
        }
    }
}
