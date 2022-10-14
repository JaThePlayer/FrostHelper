namespace FrostHelper;

[Tracked]
[CustomEntity("FrostHelper/NoMovementTrigger")]
public class NoMovementTrigger : Trigger {
    public NoMovementTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();
    }

    public static bool IsMovementDisabled(Scene scene) {
        foreach (Trigger item in scene.Tracker.SafeGetEntities<NoMovementTrigger>()) {
            if (item.Triggered) {
                return true;
            }
        }

        return false;
    }

    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded) 
            return;
        _hooksLoaded = true;

        On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
    }

    private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self) {
        if (IsMovementDisabled(self.Scene)) {
            var prevMoveX = self.moveX;
            self.moveX = 0;
            var prevMoveY = Input.MoveY.Value;
            Input.MoveX.Value = 0;
            Input.MoveY.Value = 0;

            var ret = orig(self);

            self.moveX = prevMoveX;
            Input.MoveY.Value = prevMoveY;
            Input.MoveX.Value = prevMoveX;

            return ret;
        }

        return orig(self);
    }
}
