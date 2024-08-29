using FrostHelper.Helpers;
using System.Globalization;

namespace FrostHelper;

public static class Extensions {
    public static int ToInt(this string s) => Convert.ToInt32(s, CultureInfo.InvariantCulture);
    public static int ToIntHex(this string s) => int.Parse(s, NumberStyles.HexNumber);
    public static uint ToUInt(this string s) => Convert.ToUInt32(s, CultureInfo.InvariantCulture);
    public static uint ToUIntHex(this string s) => uint.Parse(s, NumberStyles.HexNumber);
    public static uint ToUIntHex(this ReadOnlySpan<char> s) => uint.Parse(s, NumberStyles.HexNumber);
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
    /// Gets a hashset out of a comma-seperated list of elements. Each element in the hashset is trimmed
    /// </summary>
    public static HashSet<string> GetStringHashsetTrimmed(this EntityData data, string key, string def = "") {
        HashSet<string> ret = [];
        var str = data.Attr(key, def).AsSpan();
        
        var p = new SpanParser(str);
        while (p.SliceUntil(',').TryUnpack(out var entryParser)) {
            ret.Add(entryParser.ReadStr().Trim().ToString());
        }
        
        return ret;
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

    public static Vector2[] GetNodesWithOffsetWithPositionPrepended(this EntityData data, Vector2 offset) {
        var nodes = new Vector2[data.Nodes.Length + 1];
        var i = 0;
        nodes[i++] = data.Position + offset;
        foreach (var item in data.Nodes) {
            nodes[i++] = item + offset;
        }

        return nodes;
    }

    public static Vector2[] GetNodesWithOffsetWithPositionAppended(this EntityData data, Vector2 offset) {
        var nodes = new Vector2[data.Nodes.Length + 1];
        var i = 0;
        foreach (var item in data.Nodes) {
            nodes[i++] = item + offset;
        }
        nodes[i++] = data.Position + offset;

        return nodes;
    }

    public static BlendState GetBlendState(this EntityData data, string cacheKey, BlendState def) {
        if (data.Values.TryGetValue(cacheKey, out var cached))
            return (BlendState) cached;

        var state = new BlendState() {
            Name = $"fh.blend_{data.Level.Name}:{data.ID}",

            // defaults based on AlphaBlend
            AlphaBlendFunction = data.Enum("alphaBlendFunction", BlendFunction.Add),
            ColorBlendFunction = data.Enum("colorBlendFunction", BlendFunction.Add),
            ColorSourceBlend = data.Enum("colorSourceBlend", Blend.One),
            ColorDestinationBlend = data.Enum("colorDestinationBlend", Blend.InverseSourceAlpha),
            AlphaSourceBlend = data.Enum("alphaSourceBlend", Blend.One),
            AlphaDestinationBlend = data.Enum("alphaDestinationBlend", Blend.InverseSourceAlpha),
            BlendFactor = data.GetColor("blendFactor", "ffffff"),

            ColorWriteChannels = data.Enum("colorWriteChannels", ColorWriteChannels.All),
        };

        // xna sanity checks
        // todo: remove once Core is stable
        if ((state.AlphaBlendFunction is BlendFunction.Min or BlendFunction.Max) && (state.AlphaSourceBlend is not Blend.One || state.AlphaDestinationBlend is not Blend.One)) {
            NotificationHelper.Notify($"AlphaSourceBlend and AlphaDestinationBlend MUST be One when using AlphaBlendFunction Min or Max,\nor XNA will crash!");
        }

        if ((state.ColorBlendFunction is BlendFunction.Min or BlendFunction.Max) && (state.ColorSourceBlend is not Blend.One || state.ColorDestinationBlend is not Blend.One)) {
            NotificationHelper.Notify($"ColorSourceBlend and ColorDestinationBlend MUST be One when using ColorBlendFunction Min or Max,\nor XNA will crash!");
        }


        data.Values[cacheKey] = state;
        return state;
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

    public static List<Component> SafeGetComponents<T>(this Tracker t) where T : Component {
        if (t.Components.TryGetValue(typeof(T), out var components)) {
            return components;
        }

        return _emptyListComponent;
    }

    public static T? SafeGetEntity<T>(this Tracker t) where T : Entity {
        var type = typeof(T);
        if (!t.Entities.TryGetValue(type, out var list) || list.Count == 0) {
            return null;
        }

        return list[0] as T;
    }

    public static List<Entity> SafeGetEntities(this Tracker t, Type type) {
        if (t.Entities.TryGetValue(type, out var entities)) {
            return entities;
        }

        return _emptyListEntity;
    }

    public static List<Entity>? GetEntitiesOrNull(this Tracker t, Type type) {
        if (t.Entities.TryGetValue(type, out var entities)) {
            return entities;
        }

        return null;
    }

    private static readonly List<Entity> _emptyListEntity = new();
    private static readonly List<Component> _emptyListComponent = new();

    /// <summary>
    /// Calls <see cref="Entity.Add(Component)"/> with the given component, and then returns that component
    /// </summary>
    public static T AddF<T>(this Entity e, T component) where T : Component {
        e.Add(component);

        return component;
    }

    /// <summary>
    /// Forcibly removes this entity from the scene, even if it was added this frame
    /// </summary>
    public static void ForceRemoveSelf(this Entity e) {
        e.Scene?.Entities.toRemove.Add(e);
    }

    public static Point ToPoint(this Vector2 v) => new((int) v.X, (int)v.Y);

    /// <summary>
    /// Calls ToList on the given enumerable, unless it is already a List, in which case this is a no-op.
    /// As such, it does not guarantee that the returned list is a new list.
    /// </summary>
    public static List<T> ToListIfNotList<T>(this IEnumerable<T> self) => self switch {
        List<T> list => list,
        var other => other.ToList()
    };
    
    public static Session.Counter GetCounterObj(this Session session, string counterName) {
        var counters = session.Counters;
        foreach (var c in counters) {
            if (c.Key == counterName)
                return c;
        }

        var ret = new Session.Counter { Key = counterName, Value = 0 };
        session.Counters.Add(ret);
        
        return ret;
    }
}
