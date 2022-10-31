namespace FrostHelper.EXPERIMENTAL;

public static class MonocleDrawShapeFixer {

    public static Texture2D PixelTexture;

    private static List<ILHook> Hooks;
    private static bool _hooksLoaded;
    public static void Load() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        PixelTexture = Draw.Pixel.Texture.Texture_Safe;
        Hooks = new();

        foreach (var method in typeof(Draw).GetMethods()) {
            if (method.GetMethodBody() is { })
                Hooks.Add(new(method, ChangePixelGetter));
        }
    }

    public static void ChangePixelGetter(ILContext ctx) {
        var cursor = new ILCursor(ctx);

        if (cursor.TryGotoNext(MoveType.Before, 
            instr => instr.MatchLdsfld(typeof(Draw).FullName, "Pixel"),
            instr => instr.MatchCallvirt<MTexture>("get_Texture"),
            instr => instr.MatchCallvirt<VirtualTexture>("get_Texture_Safe")
        )) {
            cursor.RemoveRange(3); // remove the 3 instrs we just matched
            cursor.Emit(OpCodes.Ldsfld, typeof(MonocleDrawShapeFixer).GetField(nameof(PixelTexture))); // replace with just one static field load
        }
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        PixelTexture = null!;

        if (Hooks is { } h)
        foreach (var item in h) {
            item?.Dispose();
        }
    }
}