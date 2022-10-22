namespace FrostHelper.EXPERIMENTAL;

public static class FlaglineCull {
    private static bool _hooksLoaded;

    public static void Load() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;
        IL.Celeste.Flagline.Render += Flagline_Render_Culling;
        IL.Celeste.Wire.Render += Wire_Render_Culling;
        IL.Celeste.Cobweb.DrawCobweb += Cobweb_DrawCobweb;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;
        IL.Celeste.Flagline.Render -= Flagline_Render_Culling;
        IL.Celeste.Wire.Render -= Wire_Render_Culling;
        IL.Celeste.Cobweb.DrawCobweb -= Cobweb_DrawCobweb;
    }

    private static void Cobweb_DrawCobweb(ILContext il) {
        var cursor = new ILCursor(il);

        byte localId = GetFirstSimpleCurveLocalId(cursor);

        // inject our culling after the recursive DrawCobweb call - we want to cull each offshoot individually or we'll have pop-in.
        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<SimpleCurve>("Begin")) &&
            cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(out _))
        ) {
            cursor.Emit(OpCodes.Ldloca_S, localId);
            cursor.EmitCall(CullCheck_Cobweb);

            EmitEarlyReturnIfFalse(cursor);
        }
    }

    private static bool CullCheck_Cobweb(ref SimpleCurve curve) {
        return CameraCullHelper.IsVisible(FrostModule.GetCurrentLevel().Camera.Position, curve, 2f);
    }

    private static void Wire_Render_Culling(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStfld<SimpleCurve>("Control"))) {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(CullCheck_Wire);

            EmitEarlyReturnIfFalse(cursor);
        }
    }

    private static bool CullCheck_Wire(Wire self) {
        return CameraCullHelper.IsVisible(FrostModule.GetCurrentLevel().Camera.Position, self.Curve, 2f);
    }

    private static void Flagline_Render_Culling(ILContext il) {
        var cursor = new ILCursor(il);

        byte localId = GetFirstSimpleCurveLocalId(cursor);

        if (cursor.SeekCall<SimpleCurve>(".ctor")) {
            cursor.Emit(OpCodes.Ldloca_S, localId);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(CullCheck_Flagline);

            EmitEarlyReturnIfFalse(cursor);
        }
    }

    private static bool CullCheck_Flagline(ref SimpleCurve curve, Flagline self) {
        return CameraCullHelper.IsVisible(FrostModule.GetCurrentLevel().Camera.Position, curve, self.clothes.Max(clothLen) + 2f);
    }

    private static Func<Flagline.Cloth, int> clothLen = c => c.Height + c.Length;

    private static byte GetFirstSimpleCurveLocalId(ILCursor cursor) {
        return (byte) cursor.Body.Variables.First(v => v.VariableType.Name.Contains("SimpleCurve")).Index;
    }

    private static void EmitEarlyReturnIfFalse(ILCursor cursor) {
        var lbl = cursor.DefineLabel();
        cursor.Emit(OpCodes.Brtrue, lbl);
        cursor.Emit(OpCodes.Ret);
        cursor.MarkLabel(lbl);
    }
}
