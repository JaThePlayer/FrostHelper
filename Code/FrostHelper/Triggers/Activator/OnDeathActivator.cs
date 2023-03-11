namespace FrostHelper.Triggers.Activator;

[Tracked]
[CustomEntity("FrostHelper/OnDeathActivator")]
internal class OnDeathActivator : BaseActivator {
    private static bool _hooksLoaded = false;

    public static void LoadHooksIfNeeded() {
        if (_hooksLoaded) 
            return;

        Everest.Events.Player.OnDie += Player_OnDie;

        _hooksLoaded = true;
    }

    private static void Player_OnDie(Player player) {
        foreach (OnDeathActivator activator in player.Scene.Tracker.SafeGetEntities<OnDeathActivator>()) {
            activator.ActivateAll(player);
        }
    }

    [OnUnload]
    public static void Unload() {
        if (!_hooksLoaded)
            return;

        _hooksLoaded = false;
    }

    public OnDeathActivator(EntityData data, Vector2 offset) : base(data, offset) {
        LoadHooksIfNeeded();
    }
}
