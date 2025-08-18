using FrostHelper.Helpers;

namespace FrostHelper;

[Tracked]
[CustomEntity("FrostHelper/NoMovementTrigger")]
internal sealed class NoMovementTrigger : Trigger {
    private readonly ConditionHelper.Condition _condition;
    private readonly bool _mustBeInside;
    
    public NoMovementTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();

        _mustBeInside = data.Bool("mustBeInside", true);
        _condition = data.GetCondition("flag");
    }

    public static bool IsMovementDisabled(Scene scene) {
        foreach (NoMovementTrigger item in scene.Tracker.SafeGetEntities<NoMovementTrigger>()) {
            if ((!item._mustBeInside || item.Triggered) 
                && (item._condition.Empty || item._condition.Check(scene.ToLevel().Session))) {
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
