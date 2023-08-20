namespace FrostHelper.EXPERIMENTAL;
internal static class ReloadGCEdit {
    public static void Load() {
        IL.Celeste.Level.Reload += Level_Reload;
    }

    public static void Unload() {
        IL.Celeste.Level.Reload -= Level_Reload;
    }

    public static void LoadSceneTransition() {
        IL.Monocle.Engine.OnSceneTransition += Level_Reload;
    }

    public static void UnloadSceneTransition() {
        IL.Monocle.Engine.OnSceneTransition -= Level_Reload;
    }

    private static void Level_Reload(ILContext il) {
        var cursor = new ILCursor(il);

        if (cursor.SeekCall(typeof(GC), "Collect", MoveType.Before)) {
            // Remove this overly aggressive gc run:
            // GC.Collect();
            // GC.WaitForPendingFinalizers();
            cursor.Remove();
            cursor.Remove();
        }
    }
}
