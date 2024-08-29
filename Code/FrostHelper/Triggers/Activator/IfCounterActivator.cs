using FrostHelper.Helpers;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/IfCounterActivator")]
internal sealed class IfCounterActivator : BaseActivator {
    private readonly SessionCounterComparer _comparer;
    
    public IfCounterActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;
        Active = true;
        _comparer = new SessionCounterComparer(
            data.Attr("counter"),
            data.Attr("target"),
            data.Enum("operation", SessionCounterComparer.CounterOperation.Equal));
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        var level = FrostModule.TryGetCurrentLevel();
        if (level is null)
            return; // Shouldn't happen, but just in case

        if (_comparer.Check(level)) {
            ActivateAll(player);
        }
    }
}
