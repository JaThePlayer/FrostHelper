namespace FrostHelper.Helpers;
internal static class BackdropHelper {
    #region Hooks
    [OnLoad]
    public static void Load() {
        On.Celeste.Level.LoadLevel += Level_LoadLevel;
    }

    // Reset positions of backdrops if needed when the level gets loaded
    private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        orig(self, playerIntro, isFromLoader);

        ResetPositions(self);
    }

    [OnUnload]
    public static void Unload() {
        On.Celeste.Level.LoadLevel -= Level_LoadLevel;
    }
    #endregion

    internal sealed class OrigPositionData : IAttachable {
        public static string DynamicDataName => "fh.OrigPositionData";
        
        public Vector2? Pos;
    }

    public static void ResetPositions(Level? level = null) {
        level ??= FrostModule.GetCurrentLevel();
        ResetPos(level.Background);
        ResetPos(level.Foreground);

        static void ResetPos(BackdropRenderer renderer) {
            foreach (var item in renderer.Backdrops) {
                if (item.GetOrCreateDynamicDataAttached<OrigPositionData>().Pos is { } origPos) {
                    item.Position = origPos;
                }
            }
        }
    }
}
