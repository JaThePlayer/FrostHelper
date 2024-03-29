﻿namespace FrostHelper;

[Tracked]
public class FlagListener : Component {
    private static bool _hooksLoaded;

    public Action<bool> OnSet;
    public string Flag;
    public bool MustChange;
    public bool TriggerOnRoomBegin;

    public FlagListener(string flag, Action<bool> onSet, bool mustChange, bool triggerOnRoomBegin) : base(false, false) {
        LoadHooksIfNeeded();

        Flag = flag;
        OnSet = onSet;
        MustChange = mustChange;
        TriggerOnRoomBegin = triggerOnRoomBegin;
    }

    public override void EntityAwake() {
        base.EntityAwake();

        if (TriggerOnRoomBegin)
            OnSet(FrostModule.GetCurrentLevel().Session.GetFlag(Flag));
    }

    public static void LoadHooksIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        On.Celeste.Session.SetFlag += Session_SetFlag;
    }

    private static void Session_SetFlag(On.Celeste.Session.orig_SetFlag orig, Session self, string flag, bool setTo) {
        bool? prevValue = null;

        foreach (FlagListener item in Engine.Scene.Tracker.SafeGetComponents<FlagListener>()) {
            if (flag == item.Flag) {
                if (!item.MustChange || ((prevValue ??= self.GetFlag(flag)) != setTo))
                    item.OnSet(setTo);
            }  
        }

        orig(self, flag, setTo);
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Session.SetFlag -= Session_SetFlag;
    }

}
