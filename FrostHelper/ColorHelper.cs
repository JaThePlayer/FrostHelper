using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace FrostHelper
{
    public static class ColorHelper
    {
        static Dictionary<string, Color> cache = new Dictionary<string, Color>();
        static ColorHelper()
        {
            foreach (var prop in typeof(Color).GetProperties())
            {
                object value = prop.GetValue(default(Color), null);
                if (value is Color color)
                    cache[prop.Name] = color;
            }
            cache[""] = Color.White;
        }
        /// <summary>
        /// Returns a list of colors from a comma-separated string of hex colors OR xna color names
        /// </summary>
        public static Color[] GetColors(string colors)
        {
            string[] split = colors.Trim().Split(',');
            Color[] parsed = new Color[split.Length];
            for (int i = 0; i < split.Length; i++)
            {
                parsed[i] = GetColor(split[i]);
            }
            return parsed;
        }

        public static Color GetColor(string color)
        {
            if (cache.TryGetValue(color, out Color val))
                return val;

            try
            {
                val = Calc.HexToColor(color.Replace("#", ""));
                cache[color] = val;
                return val;
            }
            catch
            {
                cache[color] = Color.Transparent;
            }

            return Color.Transparent;
        }
    }
}
