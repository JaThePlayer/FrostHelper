using System.Globalization;

namespace FrostHelper;

public static class Extensions {
    public static int ToInt(this string s) => Convert.ToInt32(s, CultureInfo.InvariantCulture);
    public static int ToIntHex(this string s) => int.Parse(s, NumberStyles.HexNumber);
    public static uint ToUInt(this string s) => Convert.ToUInt32(s, CultureInfo.InvariantCulture);
    public static uint ToUIntHex(this string s) => uint.Parse(s, NumberStyles.HexNumber);
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

    /// <summary>
    /// Calls data.Attr, but uses the default value if the attribute is null or an empty string
    /// </summary>
    public static string AttrNullable(this EntityData data, string key, string def) { 
        var attr = data.Attr(key, null);
        if (string.IsNullOrEmpty(attr)) {
            return def;
        }
        return attr;
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


    public static bool ContainsReference(this Type[] self, Type type) {
        foreach (var item in self) {
            if (!ReferenceEquals(item, type)) {
                return false;
            }
        }

        return true;
    }

    public static Dictionary<string, string> GetDictionary(this EntityData data, string key) {
        // TODO: Caching
        var dict = new Dictionary<string, string>();
        string[] propertySplit = data.Attr(key, "").Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);


        foreach (var item in propertySplit) {
            int splitIndex = item.IndexOf(':');
            dict[item.Substring(0, splitIndex)] = item.Substring(splitIndex + 1);
        }

        return dict;
    }

    public static float BiggestAbsComponent(this Vector2 vec) {
        float x = Math.Abs(vec.X);
        float y = Math.Abs(vec.Y);

        return Math.Max(x, y);
    }

    public static List<Entity> SafeGetEntities<T>(this Tracker t) where T : Entity {
        return SafeGetEntities(t, typeof(T));
    }

    public static List<Entity> SafeGetEntities(this Tracker t, Type type) {
        if (!t.Entities.ContainsKey(type)) {
            return _emptyListEntity;
        }

        return t.Entities[type];
    }

    private static List<Entity> _emptyListEntity = new();
}
