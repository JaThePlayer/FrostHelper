namespace FrostHelper;

[Tracked]
public sealed class FlagListener : Component {
    private static bool _hooksLoaded;

    public Action<Session, string?, bool> OnSet;
    public string? Flag;
    public bool MustChange;
    public bool TriggerOnRoomBegin;

    // backwards compat, just in case
    public FlagListener(string? flag, Action<bool> onSet, bool mustChange, bool triggerOnRoomBegin)
        : this(flag, (_, _, val) => onSet(val), mustChange, triggerOnRoomBegin) {
    }

    public FlagListener(string? flag, Action<Session, string?, bool> onSet, bool mustChange, bool triggerOnRoomBegin) : base(false, false) {
        LoadHooksIfNeeded();

        Flag = flag;
        OnSet = onSet;
        MustChange = mustChange;
        TriggerOnRoomBegin = triggerOnRoomBegin;
    }

    public override void EntityAwake() {
        base.EntityAwake();

        if (TriggerOnRoomBegin) {
            var session = FrostModule.GetCurrentLevel().Session;
            OnSet(session, Flag, session.GetFlag(Flag));
        }
    }

    public static void LoadHooksIfNeeded() {
        if (_hooksLoaded)
            return;
        _hooksLoaded = true;

        On.Celeste.Session.SetFlag += Session_SetFlag;
    }

    private static void Session_SetFlag(On.Celeste.Session.orig_SetFlag orig, Session self, string flag, bool setTo) {
        var listeners = Engine.Scene.Tracker.SafeGetComponents<FlagListener>();
        if (listeners.Count == 0) {
            orig(self, flag, setTo);
            return;
        }
        
        bool prevValue = self.GetFlag(flag);
        orig(self, flag, setTo);

        foreach (FlagListener item in listeners) {
            if (item.Flag is null || flag == item.Flag) {
                if (!item.MustChange || (prevValue != setTo))
                    item.OnSet(self, flag, setTo);
            }  
        }
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;
        _hooksLoaded = false;

        On.Celeste.Session.SetFlag -= Session_SetFlag;
    }

}
