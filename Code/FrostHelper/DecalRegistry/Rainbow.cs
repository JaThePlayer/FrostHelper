using FrostHelper.Helpers;
using System.Xml;

namespace FrostHelper.DecalRegistry;

public static class Rainbow {
    [OnLoad]
    public static void Load() {
        Celeste.Mod.DecalRegistry.AddPropertyHandler("frosthelper.rainbow", (Decal decal, XmlAttributeCollection attrs) => {
            decal.GetOrCreateAttached<RainbowDecalMarker>();
        });
    }
}

sealed class RainbowDecalMarker {
    public RainbowDecalMarker() {
        LoadHooksIfNeeded();
    }

    private static bool _loaded;
    public static void LoadHooksIfNeeded() {
        if (!_loaded) {
            _loaded = true;
            IL.Celeste.Decal.Banner.Render += AllowColorChange;
            IL.Celeste.Decal.CoreSwapImage.Render += AllowColorChange;
            IL.Celeste.Decal.DecalImage.Render += AllowColorChange;
            IL.Celeste.Decal.FinalFlagDecalImage.Render += AllowColorChange;
        }
    }

    [OnUnload]
    public static void UnloadHooks() {
        if (!_loaded)
            return;

        IL.Celeste.Decal.Banner.Render -= AllowColorChange;
        IL.Celeste.Decal.CoreSwapImage.Render -= AllowColorChange;
        IL.Celeste.Decal.DecalImage.Render -= AllowColorChange;
        IL.Celeste.Decal.FinalFlagDecalImage.Render -= AllowColorChange;
    }

    private static void AllowColorChange(ILContext il) {
        ILCursor cursor = new ILCursor(il);

        // newer everest versions added a new "Color" field
        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdfld<Decal>("Color"))) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitCall(GetColor);
            return;
        }

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchCall<Color>("get_White"))) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitCall(GetColor);
            return;
        }
    }

    private static Color GetColor(Color orig, Component self) {
        if (self.Entity.TryGetAttached<RainbowDecalMarker>() != null) {
            return ColorHelper.GetHue(self.Scene, self.Entity.Position);
        } else {
            return orig;
        }
    }
}