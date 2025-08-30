namespace FrostHelper.TweakManagers;

internal static class TimeBasedClimbBlocker {
    public static float NoClimbTimer { 
        get => FrostModule.Session.NoClimbTimer; 
        set {
            FrostModule.Session.NoClimbTimer = value;

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

    private static void Level_Update(On.Celeste.Level.orig_Update orig, Level self) {
        orig(self);
        NoClimbTimer -= Engine.DeltaTime;
    }

    private static bool Player_ClimbCheck(On.Celeste.Player.orig_ClimbCheck orig, Player self, int dir, int yAdd) {
        if (NoClimbTimer > 0f) {
            return false;
        }
        return orig(self, dir, yAdd);
    }
}
