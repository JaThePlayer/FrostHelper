namespace FrostHelper.Components;

[Tracked]
internal sealed class PostAwakeHook(Action onAwake) : Component(true, false) {
    public readonly Action OnAwake = onAwake;
    private bool _awoken;
    
    #region Hooks
    [OnLoad]
    public static void LoadHooks() {
        On.Monocle.EntityList.UpdateLists += EntityListOnUpdateLists;
    }
    
    [OnUnload]
    public static void UnloadHooks() {
        On.Monocle.EntityList.UpdateLists -= EntityListOnUpdateLists;
    }

    private static void EntityListOnUpdateLists(On.Monocle.EntityList.orig_UpdateLists orig, EntityList self) {
        orig(self);

        foreach (PostAwakeHook c in self.Scene.Tracker.SafeGetComponents<PostAwakeHook>()) {
            if (!c._awoken) {
                c._awoken = true;
                c.OnAwake();
            }
        }
    }

    public override void Update() {
        if (_awoken)
            RemoveSelf();
    }

    #endregion
}