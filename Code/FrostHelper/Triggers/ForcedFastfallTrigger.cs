using Celeste.Mod.Helpers;

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

        if (!cursor.TryGotoNextBestFit(MoveType.After,
                instr => instr.MatchStfld<Player>(nameof(Player.maxFall)),
                instr => instr.MatchLdsfld("Celeste.Input", nameof(Input.MoveY)),
                instr => instr.MatchCall<VirtualIntegerAxis>("op_Implicit"))) {
            Logger.Log(LogLevel.Error, "FrostHelper", "Failed to apply Player_NormalUpdate() IL hook for ForcedFastfallTrigger!");
            return;
        }
        
        // - if (Input.MoveY == 1f && this.Speed.Y >= num3)
        // + if (FakeMoveY(Input.MoveY, this) == 1f && this.Speed.Y >= num3)
        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate(FakeMoveY);
    }

    private static float FakeMoveY(float orig, Player player) {
        if (IsForcedFastfall(player.Scene))
            return 1f;
        return orig;
    }
}
