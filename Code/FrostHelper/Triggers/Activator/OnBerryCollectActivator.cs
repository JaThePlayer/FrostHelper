namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnBerryCollectActivator")]
[Tracked]
internal sealed class OnBerryCollectActivator : BaseActivator {
    #region Hooks

    private static bool _hooksLoaded = false;

    internal static void LoadIfNeeded() {
        if (_hooksLoaded) return;
        _hooksLoaded = true;
        On.Celeste.Leader.LoseFollower += LeaderOnLoseFollower;
    }

    private static void LeaderOnLoseFollower(On.Celeste.Leader.orig_LoseFollower orig, Leader self, Follower follower) {
        orig(self, follower);

        if (follower.Entity is IStrawberry or IStrawberrySeeded && self.Entity is Player player) {
            // Strawberry Detach Triggers set ReturnHomeWhenLost to false, we don't want to activate
            // the activator for detached berries.
            if (follower.Entity is Strawberry { ReturnHomeWhenLost: false })
                return;
            
            foreach (OnBerryCollectActivator activator in self.Scene.Tracker.SafeGetEntities<OnBerryCollectActivator>()) {
                activator.OnBerryCollected(player);
            }
        }
    }

    [OnUnload]
    internal static void Unload() {
        if (!_hooksLoaded) return;
        _hooksLoaded = false;
        On.Celeste.Leader.LoseFollower -= LeaderOnLoseFollower;
    }
    #endregion

    internal void OnBerryCollected(Player player) {
        ActivateAll(player);
    }
    
    public OnBerryCollectActivator(EntityData data, Vector2 offset) : base(data, offset) {
        LoadIfNeeded();
        Active = false;
        Collidable = false;
    }
}