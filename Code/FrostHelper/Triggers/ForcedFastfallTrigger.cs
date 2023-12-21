using Celeste.Mod.Entities;

namespace FrostHelper;

[Tracked]
[CustomEntity("FrostHelper/ForcedFastfall")]
public class ForcedFastfallTrigger : Trigger {

    public ForcedFastfallTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();
    }

    public static bool IsForcedFastfall(Scene scene) {
        foreach (ForcedFastfallTrigger item in scene.Tracker.SafeGetEntities<ForcedFastfallTrigger>()) {
            if (item.Triggered)
                return true;
        }

        return false;
    }

    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        IL.Celeste.Player.NormalUpdate += Player_NormalUpdate;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        IL.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
    }

    private static void Player_NormalUpdate(ILContext il) {
        var cursor = new ILCursor(il);

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdcR4(Player.MaxFall))) {
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate(GetMaxFallSpeed);
            break;
        }

        while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchStloc(9))) {
            cursor.Emit(OpCodes.Ldarg_0); // this
            cursor.EmitDelegate(GetCurrentFallSpeed);
            cursor.Emit(OpCodes.Stloc_S, (byte) 9);
            break;
        }
    }

    private static float GetCurrentFallSpeed(Player player) {
        if (IsForcedFastfall(player.Scene))
            return Player.MaxFall + (Player.FastMaxFall - Player.MaxFall) * 0.5f; //200f;
        
        float maxFall = Player.MaxFall;
        float fastMaxFall = Player.FastMaxFall;
        if (player.SceneAs<Level>().InSpace) {
            maxFall *= 0.6f;
            fastMaxFall *= 0.6f;
        }

        return maxFall + (fastMaxFall - maxFall) * 0.5f;
    }

    private static float GetMaxFallSpeed(Player player) {
        return !IsForcedFastfall(player.Scene) ? Player.MaxFall : Player.FastMaxFall;
    }
}
