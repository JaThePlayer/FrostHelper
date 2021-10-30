using Celeste;
using Microsoft.Xna.Framework;
using System;
using System.Globalization;

namespace FrostHelper {
    public static class Extensions {

        public static int ToInt(this string s) => Convert.ToInt32(s, CultureInfo.InvariantCulture);
        public static int FromHex(this string s) => int.Parse(s, NumberStyles.HexNumber);
        public static uint ToUInt(this string s) => Convert.ToUInt32(s, CultureInfo.InvariantCulture);
        public static short ToShort(this string s) => Convert.ToInt16(s, CultureInfo.InvariantCulture);
        public static ushort ToUShort(this string s) => Convert.ToUInt16(s, CultureInfo.InvariantCulture);
        public static byte ToByte(this string s) => Convert.ToByte(s, CultureInfo.InvariantCulture);
        public static sbyte ToSByte(this string s) => Convert.ToSByte(s, CultureInfo.InvariantCulture);
        public static float ToSingle(this string s) => Convert.ToSingle(s, CultureInfo.InvariantCulture);
        public static double ToDouble(this string s) => Convert.ToDouble(s, CultureInfo.InvariantCulture);
        public static decimal ToDecimal(this string s) => Convert.ToDecimal(s, CultureInfo.InvariantCulture);

        public static Color GetColor(this EntityData data, string key, string defHexCode) {
            return ColorHelper.GetColor(data.Attr(key, defHexCode ?? "White"));
        }
        public static Color[] GetColors(this EntityData data, string key, Color[] def) {
            return ColorHelper.GetColors(data.Attr(key, "")) ?? def;
        }

        public static Vector2 GetVec2(this EntityData data, string key, Vector2 defaultValue, bool treatFloatAsXOnly = false) {
            string val = data.Attr(key, null);
            if (val is null) {
                return defaultValue;
            }

            int splitIndex = val.IndexOf(',');

            return splitIndex switch {
                -1 => treatFloatAsXOnly ? new(val.ToSingle(), defaultValue.Y) : new(val.ToSingle()),
                _ => new(val.Substring(0, splitIndex).ToSingle(), val.Substring(splitIndex + 1).ToSingle())
            };
        }
    }
}
