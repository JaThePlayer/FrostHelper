using FrostHelper.Helpers;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    
    public static Color? GetColorNullable(this EntityData data, string key, string defHexCode = "") {
        var str = data.Attr(key, defHexCode ?? "");
        if (string.IsNullOrWhiteSpace(str))
            return null;
        
        return ColorHelper.GetColor(str);
    }
    
    public static int? GetIntNullable(this EntityData data, string key, int? def = null) {
        if (data.Values?.TryGetValue(key, out var obj) is not true)
            return def;
        
        if (obj is int num)
            return num;
        
        if (int.TryParse(obj.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            return result;
        
        return def;
    }
    
    public static Color[] GetColors(this EntityData data, string key, Color[] def) {
        return ColorHelper.GetColors(data.Attr(key, "")) ?? def;
    }

    internal static T[] ParseArray<T>(this EntityData data, string key, char separator, T[] def) where T : ISpanParsable<T> {
        if (data.String(key) is not { } str)
            return def;

        var parser = new SpanParser(str);
        return parser.ParseList<T>(separator).ToArray();
    }
    
    internal static T Parse<T>(this EntityData data, string key, T def) where T : IDetailedParsable<T> {
        if (data.String(key) is not { } str)
            return def;

        if (T.TryParse(str, null, out var result, out var errorMessage))
            return result;

        NotificationHelper.Notify(errorMessage);
        return def;
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

        var parser = new SpanParser(val);
        if (!parser.ReadUntil<float>(',').TryUnpack(out var x)) {
            NotificationHelper.Notify($"Failed to parse {val} as a Vector2!");
            return defaultValue;
        }

        if (parser.IsEmpty) {
            return treatFloatAsXOnly ? new(x, defaultValue.Y) : new(x);
        }
        
        if (!parser.ReadUntil<float>(',').TryUnpack(out var y)) {
            NotificationHelper.Notify($"Failed to parse {val} as a Vector2!");
            return defaultValue;
        }

        return new(x, y);
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
        /*
        if ((state.AlphaBlendFunction is BlendFunction.Min or BlendFunction.Max) && (state.AlphaSourceBlend is not Blend.One || state.AlphaDestinationBlend is not Blend.One)) {
            NotificationHelper.Notify($"AlphaSourceBlend and AlphaDestinationBlend MUST be One when using AlphaBlendFunction Min or Max,\nor XNA will crash!");
        }

        if ((state.ColorBlendFunction is BlendFunction.Min or BlendFunction.Max) && (state.ColorSourceBlend is not Blend.One || state.ColorDestinationBlend is not Blend.One)) {
            NotificationHelper.Notify($"ColorSourceBlend and ColorDestinationBlend MUST be One when using ColorBlendFunction Min or Max,\nor XNA will crash!");
        }
        */

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
    
    public static Span<Entity> SafeGetEntitiesSpan<T>(this Tracker t) where T : Entity {
        return CollectionsMarshal.AsSpan(SafeGetEntities(t, typeof(T)));
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

    /// <summary>
    /// Fastpath for Image.DrawOutline(1)
    /// </summary>
    public static void DrawOutlineFast(this Image img, Color color) {
        var b = Draw.SpriteBatch;
        var texture = img.Texture.Texture.Texture_Safe;

        var drawPos = img.RenderPosition;
        var clipRect = img.Texture.ClipRect;
        var rot = img.Rotation;
        var scaleFix = img.Texture.ScaleFix;
        var origin = (img.Origin - img.Texture.DrawOffset) / scaleFix;
        var scale = img.Scale * scaleFix;
        
        b.Draw(texture, drawPos - Vector2.UnitY, clipRect, color, rot, origin, scale, SpriteEffects.None, 0f);
        b.Draw(texture, drawPos + Vector2.UnitY, clipRect, color, rot, origin, scale, SpriteEffects.None, 0f);
        b.Draw(texture, drawPos - Vector2.UnitX, clipRect, color, rot, origin, scale, SpriteEffects.None, 0f);
        b.Draw(texture, drawPos + Vector2.UnitX, clipRect, color, rot, origin, scale, SpriteEffects.None, 0f);
        b.Draw(texture, drawPos - Vector2.One, clipRect, color, rot, origin, scale, SpriteEffects.None, 0f);
        b.Draw(texture, drawPos + Vector2.One, clipRect, color, rot, origin, scale, SpriteEffects.None, 0f);
        b.Draw(texture, drawPos + new Vector2(-1f, 1f), clipRect, color, rot, origin, scale, SpriteEffects.None, 0f);
        b.Draw(texture, drawPos + new Vector2(1f, -1f), clipRect, color, rot, origin, scale, SpriteEffects.None, 0f);
    }

    public static Vector3 AddXY(this Vector3 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y, a.Z);
    
    public static NumVector2 SafeNormalize(this NumVector2 vec) => vec.SafeNormalize(default(NumVector2));

    public static NumVector2 SafeNormalize(this NumVector2 vec, float length)
    {
        return vec.SafeNormalize(default, length);
    }

    public static NumVector2 SafeNormalize(this NumVector2 vec, NumVector2 ifZero)
    {
        if (vec == default)
            return ifZero;
        return NumVector2.Normalize(vec);
    }
    
    public static NumVector2 SafeNormalize(this NumVector2 vec, NumVector2 ifZero, float length)
    {
        if (vec == default)
            return ifZero * length;
        return NumVector2.Normalize(vec) * length;
    }
    
    public static NumVector2 Perpendicular(this NumVector2 vector) => new(-vector.Y, vector.X);
    
    public static Vector2 Add(this Vector2 vector, NumVector2 other) => new(vector.X + other.X, vector.Y + other.Y);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NumVector2 ToNumerics(this Vector2 vector)
#if NET8_0_OR_GREATER
        => Unsafe.BitCast<Vector2, NumVector2>(vector);
#else
        => new(vector.X, vector.Y);
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 ToXna(this NumVector2 vector)
    #if NET8_0_OR_GREATER
        => Unsafe.BitCast<NumVector2, Vector2>(vector);
    #else
        => new(vector.X, vector.Y);
    #endif

    public static bool ParsePair(this ReadOnlySpan<char> str, char splitOn, out ReadOnlySpan<char> left,
        out ReadOnlySpan<char> right) {
        var idx = str.IndexOf(splitOn);
        if (idx == -1) {
            left = str;
            right = Span<char>.Empty;
            return false;
        }

        left = str[..idx];
        right = str[(idx + 1)..];
        return true;
    }

    public static Level ToLevel(this Scene scene) => scene switch {
        Level l => l,
        LevelLoader loader => loader.Level,
        AssetReloadHelper => (Level) AssetReloadHelper.ReturnToScene,
        _ => throw new Exception("ToLevel called outside of a level... how did you manage that?")
    };

    internal static TEnum FlagEnumFromMultipleBools<TEnum>(this EntityData data, params Span<(TEnum Value, string FieldName)> fields) where TEnum : struct, Enum {
        ulong ret = default;
        foreach (var (value, fieldName) in fields) {
            if (data.Bool(fieldName))
                ret |= value.ToInt64();
        }
        
        Unsafe.SkipInit(out TEnum result);
        Type underlyingType = typeof(TEnum).GetEnumUnderlyingType();

        if (underlyingType == typeof(sbyte)) Unsafe.As<TEnum, sbyte>(ref result) = (sbyte)ret;
        if (underlyingType == typeof(byte)) Unsafe.As<TEnum, byte>(ref result) = (byte)ret;
        if (underlyingType == typeof(short)) Unsafe.As<TEnum, short>(ref result) = (short)ret;
        if (underlyingType == typeof(ushort)) Unsafe.As<TEnum, ushort>(ref result) = (ushort)ret;
        if (underlyingType == typeof(int)) Unsafe.As<TEnum, int>(ref result) = (int)ret;
        if (underlyingType == typeof(uint)) Unsafe.As<TEnum, uint>(ref result) = (uint)ret;
        if (underlyingType == typeof(long)) Unsafe.As<TEnum, long>(ref result) = (long)ret;
        if (underlyingType == typeof(ulong)) Unsafe.As<TEnum, ulong>(ref result) = (ulong)ret;

        return result;
    }

    internal static ulong ToInt64<TEnum>(this TEnum value) where TEnum : struct, Enum {
        Type underlyingType = typeof(TEnum).GetEnumUnderlyingType();
        if (underlyingType == typeof(sbyte)) return (ulong)Unsafe.BitCast<TEnum, sbyte>(value);
        if (underlyingType == typeof(byte)) return (ulong)Unsafe.BitCast<TEnum, byte>(value);
        if (underlyingType == typeof(short)) return (ulong)Unsafe.BitCast<TEnum, short>(value);
        if (underlyingType == typeof(ushort)) return (ulong)Unsafe.BitCast<TEnum, ushort>(value);
        if (underlyingType == typeof(int)) return (ulong)Unsafe.BitCast<TEnum, int>(value);
        if (underlyingType == typeof(uint)) return (ulong)Unsafe.BitCast<TEnum, uint>(value);
        if (underlyingType == typeof(long)) return (ulong)Unsafe.BitCast<TEnum, long>(value);
        if (underlyingType == typeof(ulong)) return (ulong)Unsafe.BitCast<TEnum, ulong>(value);

        ThrowCannotConvertToInt64Exception(value);
        return 0;
    }

    private static void ThrowCannotConvertToInt64Exception<TEnum>(TEnum value) where TEnum : struct, Enum {
        throw new InvalidOperationException($"Cannot convert {value} [{typeof(TEnum)}] to Int64");
    }

    internal static Rectangle GetAbsRect(this Hitbox h) {
        var p = h.AbsolutePosition;
        return new Rectangle((int)p.X, (int)p.Y, (int)h.Width, (int)h.Height);
    }
}
