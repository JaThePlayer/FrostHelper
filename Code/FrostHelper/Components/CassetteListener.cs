namespace FrostHelper.Components;

// from the SJ code repo, + support for index == -1
[Tracked]
internal class CassetteListener : Component {
    public static Color ColorFromCassetteIndex(int index) => index switch {
        0 => Calc.HexToColor("49aaf0"),
        1 => Calc.HexToColor("f049be"),
        2 => Calc.HexToColor("fcdc3a"),
        3 => Calc.HexToColor("38e04e"),
        _ => Color.White
    };

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    public Action? OnFinish;
    public Action? OnWillToggle;
    public Action? OnActivated;
    public Action? OnDeactivated;
    public Action<bool>? OnSilentUpdate;
    public Action<int, bool>? OnTick;

    public int Index;
    public bool Activated;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

    private CassetteBlockManager cassetteBlockManager;

    public CassetteListener(int index) : base(false, false) {
        Index = index;

        LoadHooksIfNeeded();
    }

    public override void EntityAdded(Scene scene) {
        base.EntityAdded(scene);

        if (scene is not Level level)
            return;
        level.HasCassetteBlocks = true;

        cassetteBlockManager = scene.Tracker.GetEntity<CassetteBlockManager>() ?? scene.Entities.ToAdd.FirstOfTypeOrDefault<Entity, CassetteBlockManager>()!;
        if (cassetteBlockManager == null)
            scene.Add(cassetteBlockManager = new CassetteBlockManager());
    }

    public override void EntityRemoved(Scene scene) {
        base.EntityRemoved(scene);
        cassetteBlockManager = null!;
    }

    static bool _hooksLoaded = false;

    [HookPreload]
    public static void LoadHooksIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        IL.Celeste.CassetteBlockManager.AdvanceMusic += CassetteBlockManager_AdvanceMusic;
        On.Celeste.CassetteBlockManager.StopBlocks += CassetteBlockManager_StopBlocks;
        On.Celeste.CassetteBlockManager.SilentUpdateBlocks += CassetteBlockManager_SilentUpdateBlocks;
        On.Celeste.CassetteBlockManager.SetActiveIndex += CassetteBlockManager_SetActiveIndex;
        On.Celeste.CassetteBlockManager.SetWillActivate += CassetteBlockManager_SetWillActivate;
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        IL.Celeste.CassetteBlockManager.AdvanceMusic -= CassetteBlockManager_AdvanceMusic;
        On.Celeste.CassetteBlockManager.StopBlocks -= CassetteBlockManager_StopBlocks;
        On.Celeste.CassetteBlockManager.SilentUpdateBlocks -= CassetteBlockManager_SilentUpdateBlocks;
        On.Celeste.CassetteBlockManager.SetActiveIndex -= CassetteBlockManager_SetActiveIndex;
        On.Celeste.CassetteBlockManager.SetWillActivate -= CassetteBlockManager_SetWillActivate;
    }

    private static void CassetteBlockManager_AdvanceMusic(ILContext il) {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.AfterLabel,
            instr => instr.MatchLdarg(0),
            instr => instr.MatchLdfld<CassetteBlockManager>("leadBeats"),
            instr => instr.MatchLdcI4(0))) {

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitCall(AdvanceMusicCallback);
        }
    }

    private static void AdvanceMusicCallback(CassetteBlockManager self) {
        if (self.beatIndex % self.beatsPerTick == 0 &&
            self.beatIndex % (self.beatsPerTick * self.ticksPerSwap) != 0 &&
            self.Scene is not null) {

            foreach (CassetteListener component in self.Scene.Tracker.SafeGetComponents<CassetteListener>()) {
                component.OnTick?.Invoke(self.currentIndex, false);
            }
        }
    }

    private static void CassetteBlockManager_StopBlocks(On.Celeste.CassetteBlockManager.orig_StopBlocks orig, CassetteBlockManager self) {
        orig(self);
        if (self.Scene == null)
            return;

        foreach (CassetteListener component in self.Scene.Tracker.SafeGetComponents<CassetteListener>()) {
            component.OnFinish?.Invoke();
        }
    }

    private static void CassetteBlockManager_SilentUpdateBlocks(On.Celeste.CassetteBlockManager.orig_SilentUpdateBlocks orig, CassetteBlockManager self) {
        orig(self);
        if (self.Scene == null)
            return;

        foreach (CassetteListener component in self.Scene.Tracker.SafeGetComponents<CassetteListener>()) {
            component.OnSilentUpdate?.Invoke(component.Index == -1 || component.Index == self.currentIndex);
        }
    }

    private static void CassetteBlockManager_SetActiveIndex(On.Celeste.CassetteBlockManager.orig_SetActiveIndex orig, CassetteBlockManager self, int index) {
        orig(self, index);
        if (self.Scene == null)
            return;


        foreach (CassetteListener component in self.Scene.Tracker.SafeGetComponents<CassetteListener>()) {
            if (component.Index == -1) {
                // new in frost helper
                component.OnDeactivated?.Invoke();
                component.OnActivated?.Invoke();
            } else {
                if (component.Activated && component.Index != index) {
                    component.Activated = false;
                    component.OnDeactivated?.Invoke();
                } else if (!component.Activated && component.Index == index) {
                    component.Activated = true;
                    component.OnActivated?.Invoke();
                }
            }



            component.OnTick?.Invoke(index, true);
        }
    }

    private static void CassetteBlockManager_SetWillActivate(On.Celeste.CassetteBlockManager.orig_SetWillActivate orig, CassetteBlockManager self, int index) {
        orig(self, index);
        if (self.Scene == null)
            return;

        foreach (CassetteListener component in self.Scene.Tracker.SafeGetComponents<CassetteListener>()) {
            if (component.Index == index || component.Activated) {
                component.OnWillToggle?.Invoke();
            }
        }
    }
}
