namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Activates after a delay. The simplest activator
/// </summary>
[CustomEntity("FrostHelper/DelayActivator")]
internal sealed class DelayActivator : BaseActivator {
    public DelayActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        ActivateAll(player);
    }
}
