namespace FrostHelper;

public static class TimeBasedClimbBlocker {
    private static float _NoClimbTimer;

    public static float NoClimbTimer { 
        get => _NoClimbTimer; 
        set {
            _NoClimbTimer = value;

            LoadIfNeeded();
        } 
    }

    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        On.Celeste.Player.ClimbCheck += Player_ClimbCheck;
        On.Celeste.Level.Update += Level_Update;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Player.ClimbCheck -= Player_ClimbCheck;
        On.Celeste.Level.Update -= Level_Update;
    }

    private static void Level_Update(On.Celeste.Level.orig_Update orig, Celeste.Level self) {
        orig(self);
        NoClimbTimer -= Engine.DeltaTime;
    }

    private static bool Player_ClimbCheck(On.Celeste.Player.orig_ClimbCheck orig, Celeste.Player self, int dir, int yAdd) {
        if (NoClimbTimer > 0f) {
            return false;
        }
        return orig(self, dir, yAdd);
    }
}
