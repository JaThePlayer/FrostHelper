﻿using FrostHelper.Helpers;
using System.Globalization;

namespace FrostHelper;

public static class ColorHelper {
    static readonly Dictionary<string, Color> Cache = new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, Color[]> ColorArrayCache = new(StringComparer.OrdinalIgnoreCase);
    static ColorHelper() {
        foreach (var prop in typeof(Color).GetProperties()) {
            object value = prop.GetValue(default(Color), null)!;
            if (value is Color color)
                Cache[prop.Name] = color;
        }
        Cache[""] = Color.White;
        ColorArrayCache[""] = null!;
    }
    /// <summary>
    /// Returns a list of colors from a comma-separated string of hex colors OR xna color names
    /// </summary>
    public static Color[] GetColors(string colors) {
        if (ColorArrayCache.TryGetValue(colors, out var val))
            return val;

        string[] split = colors.Trim().Split(',');
        Color[] parsed = new Color[split.Length];
        for (int i = 0; i < split.Length; i++) {
            parsed[i] = GetColor(split[i]);
        }

        ColorArrayCache[colors] = parsed;
        return parsed;
    }

    public static Color GetColor(string color) {
        if (Cache.TryGetValue(color, out Color val))
            return val;
        
        if (TryHexToColor(color, out var parsed))
            return parsed;
            
        NotificationHelper.Notify(new(LogLevel.Error, $"Invalid color: {color}"));
        parsed = Color.Transparent;
        // Don't log again for the same color
        Cache[color] = Color.Transparent;

        return parsed;
    }

    public static Color Clone(this Color c, float a) {
        return new Color(c.R, c.G, c.B, a);
    }

    /// <summary>
    /// Same as Calc.HexToColor, but supports transparency
    /// </summary>
    /// <param name="hex">A hex code representation of the color, as RGB or RGBA</param>
    /// <returns></returns>
    public static Color HexToColor(string hex) {
        if (TryHexToColor(hex, out var parsed)) {
            return parsed;
        }

        return default;
    }

    public static bool TryHexToColor(ReadOnlySpan<char> hexSpan, out Color color) {
        if (hexSpan[0] == '#') {
            hexSpan = hexSpan[1..];
        }
        hexSpan = hexSpan.Trim();

        if (!uint.TryParse(hexSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var packedValue)) {
            color = default;
            return false;
        }
        
        (bool ret, color) = hexSpan.Length switch {
            // allow 7-length as RGB because of Temple of Zoom from SC having 00bc000 as spinner tint... why
            6 or 7 => (true, new Color((byte) (packedValue >> 16), (byte) (packedValue >> 8), (byte) packedValue)), //rgb
            8 => (true, new Color((byte) (packedValue >> 24), (byte) (packedValue >> 16), (byte) (packedValue >> 8), (byte) packedValue)), // rgba
            _ => (false, default),
        };

        return ret;
    }

    public static string ColorToHex(Color color) {
        return $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
    }

    // Based on Communal Helper:

    // Used to maintain compatibility with Maddie's Helping Hand RainbowSpinnerColorController
    private static CrystalStaticSpinner? crystalSpinner;

    public static void SetGetHueScene(Scene scene) {
        crystalSpinner ??= new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);

        crystalSpinner.Scene = scene;
    }

    /// <summary>
    /// Make sure to call SetGetHueScene beforehand!
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Color GetHue(Vector2 position) {
        return crystalSpinner!.GetHue(position);
    }

    public static Color GetHue(Scene scene, Vector2 position) {
        crystalSpinner ??= new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);
        crystalSpinner.Scene = scene;

        return crystalSpinner.GetHue(position);
    }
}

internal readonly struct RGBAOrXnaColor : ISpanParsable<RGBAOrXnaColor> {
    public Color Color { get; init; }
    
    public static RGBAOrXnaColor Parse(string s, IFormatProvider? provider) 
        => Parse(s.AsSpan(), provider);

    public static bool TryParse(string? s, IFormatProvider? provider, out RGBAOrXnaColor result) =>
        TryParse(s.AsSpan(), provider, out result);

    public static RGBAOrXnaColor Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (!TryParse(s, provider, out var parsed))
        {
            throw new Exception($"Invalid RGBA color: {s}");
        }

        return parsed;
    }

    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out RGBAOrXnaColor result) {
        result = new() { Color = ColorHelper.GetColor(s.ToString()) };

        return true;
    }
}