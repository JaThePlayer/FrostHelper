namespace FrostHelper;

public static class BloomColorChange {
    /// <summary>
    /// Exposed by the API:
    /// - <see cref="API.API.GetBloomColor"/>
    /// - <see cref="API.API.SetBloomColor(Color)"/>
    /// </summary>
    internal static Color Color {
        get => FrostModule.Session.BloomColor;
        set => FrostModule.Session.BloomColor = value;
    }

    internal static Func<Color, Color> ColorManipulator = (c) => c;

    private static bool _hooksLoaded;

    [OnLoad]
    public static void Load() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        IL.Celeste.BloomRenderer.Apply += BloomRenderer_Apply;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        IL.Celeste.BloomRenderer.Apply -= BloomRenderer_Apply;
    }

    private static void BloomRenderer_Apply(ILContext il) {
        ILCursor cursor = new(il);

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White"))) {
            cursor.EmitCall(ChangeBloomColor);
        }
    }

    private static Color ChangeBloomColor(Color prev) {
        return ColorManipulator(Color);
    }
}
