namespace FrostHelper.Helpers;

internal static class CustomBackdropBlendModeHelper {
    #region Hooks
    private static bool _hooksLoaded = false;

    internal static void LoadHooksIfNeeded() {
        if (_hooksLoaded) {
            return;
        }
        _hooksLoaded = true;

        IL.Celeste.BackdropRenderer.Render += BackdropRenderer_Render;
    }

    private static void BackdropRenderer_Render(ILContext il) {
        var cursor = new ILCursor(il);

        int blendStateLoc = -1;
        if (!cursor.TryGotoNext(MoveType.After, 
            instr => instr.MatchLdsfld<BlendState>(nameof(BlendState.AlphaBlend)),
            instr => instr.MatchStloc(out blendStateLoc))) {
            return;
        }

        int backdropLoc = -1;

        if (!cursor.TryGotoNext(MoveType.Before,
            instr => instr.MatchLdloc(out backdropLoc),
            instr => instr.MatchIsinst<Parallax>()
            )) {
            return;
        }

        cursor.Emit(OpCodes.Ldloc, backdropLoc);
        cursor.Emit(OpCodes.Ldloc, blendStateLoc);
        cursor.EmitCall(ChangeBlendState);
        cursor.Emit(OpCodes.Stloc, blendStateLoc);
    }

    private static BlendState ChangeBlendState(Backdrop backdrop, BlendState prevBlend) {
        if (backdrop.TryGetAttached<BlendModeAttachedData>() is { } data && data.State != prevBlend) {
            var renderer = backdrop.Renderer;

            renderer.EndSpritebatch();

            return data.State;
        }

        return prevBlend;
    }

    [OnUnload]
    internal static void UnloadHooks() {
        if (!_hooksLoaded) {
            return;
        }
        _hooksLoaded = false;

        IL.Celeste.BackdropRenderer.Render -= BackdropRenderer_Render;

    }
    #endregion

    private class BlendModeAttachedData {
        public BlendState State;

        public BlendModeAttachedData(BlendState state) {
            State = state;

            LoadHooksIfNeeded();
        }
    }

    public static void SetBlendMode(Backdrop backdrop, BlendState state) {
        if (backdrop is Parallax parallax) {
            parallax.BlendState = state;
        } else {
            backdrop.SetAttached(new BlendModeAttachedData(state));
        }
    }

    public static BlendState? GetBlendMode(Backdrop backdrop) {
        if (backdrop is Parallax parallax) {
            return parallax.BlendState;
        } else if (backdrop.TryGetAttached<BlendModeAttachedData>() is { State: var state }) {
            return state;
        }

        return null;
    }
}
