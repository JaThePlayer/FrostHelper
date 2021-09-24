using Celeste;
using Microsoft.Xna.Framework;

namespace FrostHelper
{
    public static class Extensions
    {
        public static Color GetColor(this EntityData data, string key, string defHexCode)
        {
            return ColorHelper.GetColor(data.Attr(key, defHexCode ?? "White"));
        }
        public static Color[] GetColors(this EntityData data, string key, Color[] def)
        {
            return ColorHelper.GetColors(data.Attr(key, "")) ?? def;
        }
    }
}
