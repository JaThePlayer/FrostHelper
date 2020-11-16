using Celeste;
using Celeste.Mod.Entities;
using Monocle;
using Microsoft.Xna.Framework;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/CustomFlutterBird")]
    public class CustomFlutterBird : FlutterBird
    {
        //public static FieldInfo FlutterBird_colors

        public CustomFlutterBird(EntityData data, Vector2 offset) : base(data, offset)
        {
            Get<Sprite>().Color = Calc.Random.Choose(ColorHelper.GetColors(data.Attr("colors", "89fbff,f0fc6c,f493ff,93baff")));
        }
    }
}
