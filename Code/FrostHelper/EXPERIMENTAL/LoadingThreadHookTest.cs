namespace FrostHelper.EXPERIMENTAL;
internal static class LoadingThreadHookTest {
    //[OnLoad]
    public static void Load() {
        //#warning REMOVE
        //On.Celeste.LevelLoader.LoadingThread += LevelLoader_LoadingThread_CrashTest;
    }

    private static void LevelLoader_LoadingThread_CrashTest(On.Celeste.LevelLoader.orig_LoadingThread orig, LevelLoader self) {
        Console.WriteLine("LOADING THREAD HOOK");
        
        MapData mapData = self.session.MapData;
        var controllerInMap = mapData.Levels.Any(l => l.Entities.Any(e => e.Name == "<redacted>/<redacted>"));
        orig(self);
    }
}
