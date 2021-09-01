using Monocle;
using System.Linq;

namespace FrostHelper
{
    public class Rainbowifier : Component
    {
        public Rainbowifier() : base(false, true) { }

        public override void Render()
        {
            ColorHelper.SetGetHueScene(Scene);
            for (int i = 0; i < Entity.Components.Count; i++)
            {
                if (Entity.Components[i] is Image img)
                {
                    img.Color = ColorHelper.GetHue(img.RenderPosition);
                }
            }
        }
    }
}
