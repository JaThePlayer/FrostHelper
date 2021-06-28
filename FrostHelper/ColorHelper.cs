using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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

        // From Communal Helper:

        // Used to maintain compatibility with Max's Helping Hand RainbowSpinnerColorController
        private static CrystalStaticSpinner crystalSpinner;
        public static Color GetHue(Scene scene, Vector2 position)
        {
            if (crystalSpinner == null)
                crystalSpinner = new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);

            return _getHue(scene, position);
        }

        private static Func<Scene, Vector2, Color> _getHue = GetHueIL();

        #region Hooks

        private static Func<Scene, Vector2, Color> GetHueIL()
        {
            string methodName = "ColorHelper._getHue";

            DynamicMethodDefinition method = new DynamicMethodDefinition(methodName, typeof(Color), new[] { typeof(Scene), typeof(Vector2) });

            var gen = method.GetILGenerator();

            FieldInfo crystalSpinner = typeof(ColorHelper).GetField(nameof(ColorHelper.crystalSpinner), BindingFlags.NonPublic | BindingFlags.Static);

            // ColorHelper.crystalSpinner.Scene = scene;
            gen.Emit(OpCodes.Ldsfld, crystalSpinner);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Call, typeof(Entity).GetProperty("Scene").GetSetMethod(true));

            // return ColorHelper.crystalSpinner.GetHue(position);
            gen.Emit(OpCodes.Ldsfld, crystalSpinner);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Call, typeof(CrystalStaticSpinner).GetMethod("GetHue", BindingFlags.NonPublic | BindingFlags.Instance));
            gen.Emit(OpCodes.Ret);

            return (Func<Scene, Vector2, Color>)method.Generate().CreateDelegate(typeof(Func<Scene, Vector2, Color>));
        }

        #endregion
    }
}
