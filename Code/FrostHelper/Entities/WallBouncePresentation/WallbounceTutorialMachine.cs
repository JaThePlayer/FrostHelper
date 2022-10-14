using FMOD.Studio;

namespace FrostHelper.Entities.WallBouncePresentation;

[CustomEntity("FrostHelper/WallbounceTutorial")]
public class WallbounceTutorialMachine : WaveDashTutorialMachine {
    public string DialogKeyPrefix;
    public string GraphicsKeyPrefix;
    public string PlaybackKeyPrefix;

    public WallbounceTutorialMachine(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();

        DialogKeyPrefix = data.Attr("dialogKeyPrefix", "WAVEDASH");
        GraphicsKeyPrefix = data.Attr("graphicsKeyPrefix", "");
        PlaybackKeyPrefix = data.Attr("playbackKeyPrefix", "");
    }

    private static bool _hooksLoaded;

    [HookPreload]
    public static void LoadIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;
        On.Celeste.WaveDashTutorialMachine.OnInteract += WaveDashTutorialMachine_OnInteract;
        On.Celeste.WaveDashTutorialMachine.SkipInteraction += WaveDashTutorialMachine_SkipInteraction;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.WaveDashTutorialMachine.OnInteract -= WaveDashTutorialMachine_OnInteract;
        On.Celeste.WaveDashTutorialMachine.SkipInteraction -= WaveDashTutorialMachine_SkipInteraction;
    }

    private static void WaveDashTutorialMachine_OnInteract(On.Celeste.WaveDashTutorialMachine.orig_OnInteract orig, WaveDashTutorialMachine self, Player player) {
        if (self is WallbounceTutorialMachine wbMachine) {
            if (!wbMachine.inCutscene) {
                Level level = (self.Scene as Level)!;
                if (wbMachine.usingSfx != null) {
                    Audio.SetParameter(wbMachine.usingSfx, "end", 1f);
                    Audio.Stop(wbMachine.usingSfx, true);
                }
                wbMachine.inCutscene = true;
                wbMachine.interactStartZoom = level.ZoomTarget;
                level.StartCutscene(new Action<Level>(wbMachine.SkipInteraction), true, false, false);
                self.Add(wbMachine.routine = new Coroutine(wbMachine.NewInteractRoutine(player), true));
            }
        } else {
            orig(self, player);
        }
    }

    private static void WaveDashTutorialMachine_SkipInteraction(On.Celeste.WaveDashTutorialMachine.orig_SkipInteraction orig, WaveDashTutorialMachine self, Level level) {
        if (self is WallbounceTutorialMachine wbMachine) {
            wbMachine.wallbouncePresentation?.RemoveSelf();
            wbMachine.wallbouncePresentation = null!;
            orig(self, level);
        } else {
            orig(self, level);
        }
    }

    private WallbouncePresentation wallbouncePresentation;

    private IEnumerator NewInteractRoutine(Player player) {
        Level level = (Scene as Level)!;
        player.StateMachine.State = 11;
        player.StateMachine.Locked = true;
        yield return CutsceneEntity.CameraTo(new Vector2(X, Y - 30f) - new Vector2(160f, 90f), 0.25f, Ease.CubeOut, 0f);
        yield return level.ZoomTo(new Vector2(160f, 90f), 10f, 1f);
        usingSfx = Audio.Play("event:/state/cafe_computer_active", player.Position);
        Audio.Play("event:/new_content/game/10_farewell/cafe_computer_on", player.Position);
        Audio.Play("event:/new_content/game/10_farewell/cafe_computer_startupsfx", player.Position);
        wallbouncePresentation = new WallbouncePresentation(usingSfx, DialogKeyPrefix, GraphicsKeyPrefix, PlaybackKeyPrefix);
        Scene.Add(wallbouncePresentation);
        while (wallbouncePresentation.Viewing) {
            yield return null;
        }
        yield return level.ZoomTo(new Vector2(160f, 90f), interactStartZoom, 1f);
        player.StateMachine.Locked = false;
        player.StateMachine.State = 0;
        inCutscene = false;
        level.EndCutscene();
        Audio.SetAltMusic(null);
        yield break;
    }
}
