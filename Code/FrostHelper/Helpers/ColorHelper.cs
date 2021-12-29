namespace FrostHelper;

public static class ColorHelper {
    static Dictionary<string, Color> cache = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
    static Dictionary<string, Color[]> colorArrayCache = new Dictionary<string, Color[]>(StringComparer.OrdinalIgnoreCase);
    static ColorHelper() {
        foreach (var prop in typeof(Color).GetProperties()) {
            object value = prop.GetValue(default(Color), null);
            if (value is Color color)
                cache[prop.Name] = color;
        }
        cache[""] = Color.White;
        colorArrayCache[""] = null;
    }
    /// <summary>
    /// Returns a list of colors from a comma-separated string of hex colors OR xna color names
    /// </summary>
    public static Color[] GetColors(string colors) {
        if (colorArrayCache.TryGetValue(colors, out Color[] val))
            return val;

        string[] split = colors.Trim().Split(',');
        Color[] parsed = new Color[split.Length];
        for (int i = 0; i < split.Length; i++) {
            parsed[i] = GetColor(split[i]);
        }

        colorArrayCache[colors] = parsed;
        return parsed;
    }

    public static Color GetColor(string color) {
        if (cache.TryGetValue(color, out Color val))
            return val;

        try {
            val = HexToColor(color);
            //cache[color] = val;
            return val;
        } catch {
            //cache[color] = Color.Transparent;
        }

        return Color.Transparent;
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
        if (hex.StartsWith("#")) {
            hex = hex.Substring(1);
        }

        var packedValue = hex.ToUIntHex();
        return hex.Trim().Length switch {
            // allow 7-length as RGB because of Temple of Zoom from SC having 00bc000 as spinner tint... why
            6 or 7 => new Color((byte) (packedValue >> 16), (byte) (packedValue >> 8), (byte) packedValue), //rgb
            8 => new Color((byte) (packedValue >> 24), (byte) (packedValue >> 16), (byte) (packedValue >> 8), (byte) packedValue), // rgba
            _ => default,
        };
    }

    public static string ColorToHex(Color color) {
        return $"{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
    }

    // Based on Communal Helper:

    // Used to maintain compatibility with Max's Helping Hand RainbowSpinnerColorController
    private static CrystalStaticSpinner crystalSpinner;

    public static void SetGetHueScene(Scene scene) {
        crystalSpinner ??= new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);

        _setGetHueScene(scene);
    }

    /// <summary>
    /// Make sure to call SetGetHueScene beforehand!
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public static Color GetHue(Vector2 position) {
        return _getHueNoSetScene(position);
    }

    public static Color GetHue(Scene scene, Vector2 position) {
        crystalSpinner ??= new CrystalStaticSpinner(Vector2.Zero, false, CrystalColor.Rainbow);

        return _getHue(scene, position);
    }

    private static Func<Scene, Vector2, Color> _getHue = GetHueIL();
    private static Func<Vector2, Color> _getHueNoSetScene = GetHueNoSetSceneIL();
    private static Action<Scene> _setGetHueScene = GetSetGetHueSceneIL();

    #region ILGeneration

    private static Func<Scene, Vector2, Color> GetHueIL() {
        string methodName = "ColorHelper._getHue";

        DynamicMethodDefinition method = new DynamicMethodDefinition(methodName, typeof(Color), new[] { typeof(Scene), typeof(Vector2) });
        var gen = method.GetILProcessor();

        // ColorHelper.crystalSpinner.Scene = scene;
        EmitSetScene(gen, 0);

        // return ColorHelper.crystalSpinner.GetHue(position);
        EmitCallGetHueAndReturn(gen, 1);

        return (Func<Scene, Vector2, Color>) method.Generate().CreateDelegate(typeof(Func<Scene, Vector2, Color>));
    }

    private static Func<Vector2, Color> GetHueNoSetSceneIL() {
        string methodName = "ColorHelper._getHueNoSetScene";

        DynamicMethodDefinition method = new DynamicMethodDefinition(methodName, typeof(Color), new[] { typeof(Vector2) });

        var gen = method.GetILProcessor();

        // return ColorHelper.crystalSpinner.GetHue(position);
        EmitCallGetHueAndReturn(gen, 0);

        return method.Generate().CreateDelegate<Func<Vector2, Color>>();
    }

    private static Action<Scene> GetSetGetHueSceneIL() {
        string methodName = "ColorHelper._setGetHueScene";

        DynamicMethodDefinition method = new DynamicMethodDefinition(methodName, null, new[] { typeof(Scene) });

        var gen = method.GetILProcessor();

        // ColorHelper.crystalSpinner.Scene = scene;
        EmitSetScene(gen, 0);

        gen.Emit(OpCodes.Ret);

        return (Action<Scene>) method.Generate().CreateDelegate(typeof(Action<Scene>));
    }

    private static void EmitCallGetHueAndReturn(ILProcessor gen, int argNum) {
        FieldInfo crystalSpinner = typeof(ColorHelper).GetField(nameof(ColorHelper.crystalSpinner), BindingFlags.NonPublic | BindingFlags.Static);
        gen.LoadStaticField(crystalSpinner);
        gen.LoadArg(argNum);
        gen.EmitCall(typeof(CrystalStaticSpinner).GetMethod("GetHue", BindingFlags.NonPublic | BindingFlags.Instance));
        gen.Ret();
    }

    private static void EmitSetScene(ILProcessor gen, int argNum) {
        FieldInfo crystalSpinner = typeof(ColorHelper).GetField(nameof(ColorHelper.crystalSpinner), BindingFlags.NonPublic | BindingFlags.Static);
        gen.LoadStaticField(crystalSpinner);
        gen.LoadArg(argNum);
        gen.EmitCall(typeof(Entity).GetProperty("Scene").GetSetMethod(true));
    }

    #endregion
}
