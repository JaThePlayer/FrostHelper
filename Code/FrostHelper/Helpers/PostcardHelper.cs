namespace FrostHelper.Helpers;

internal static class PostcardHelper {
    public static void Start(string msg) {
        Audio.SetMusic(null);
        LevelEnter.ErrorMessage = msg;
        var level = FrostModule.TryGetCurrentLevel();
        LevelEnter.Go(new Session(level?.Session.Area ?? default), fromSaveData: false);
    }
}