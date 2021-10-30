using Monocle;

namespace FrostHelper {
    public static class TimeBasedClimbBlocker {
        public static float NoClimbTimer;

        [OnLoad]
        public static void Load() {
            On.Celeste.Player.ClimbCheck += Player_ClimbCheck;
            On.Celeste.Level.Update += Level_Update;
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

        [OnUnload]
        public static void Unload() {
            On.Celeste.Player.ClimbCheck -= Player_ClimbCheck;
            On.Celeste.Level.Update -= Level_Update;
        }
    }
}
