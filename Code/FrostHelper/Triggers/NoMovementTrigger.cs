namespace FrostHelper;

[Tracked]
[CustomEntity("FrostHelper/NoMovementTrigger")]
public class NoMovementTrigger : Trigger {
    public NoMovementTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

    [OnLoad]
    public static void Load() {
        On.Celeste.Player.NormalUpdate += Player_NormalUpdate;
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Player.NormalUpdate -= Player_NormalUpdate;
    }

    private static int Player_NormalUpdate(On.Celeste.Player.orig_NormalUpdate orig, Player self) {
        if (IsMovementDisabled(self.Scene)) {
            var prevMoveX = (int) self.GetValue("moveX");
            self.SetValue("moveX", 0);
            var prevMoveY = Input.MoveY.Value;
            Input.MoveX.Value = 0;
            Input.MoveY.Value = 0;

            var ret = orig(self);

            self.SetValue("moveX", prevMoveX);
            Input.MoveY.Value = prevMoveY;
            Input.MoveX.Value = prevMoveX;

            return ret;
        }


        return orig(self);
    }

    public static bool IsMovementDisabled(Scene scene) {
        foreach (Trigger item in scene.Tracker.SafeGetEntities<NoMovementTrigger>()) {
            if (item.Triggered) {
                return true;
            }
        }

        return false;
    }
}
