using FrostHelper.Helpers;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnCounterActivator")]
internal sealed class OnCounterActivator : BaseActivator {
    private readonly SessionCounterComparer _comparer;
    private bool _conditionMetLastFrame;
    
    public OnCounterActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;
        Active = true;
        _comparer = new SessionCounterComparer(
            data.Attr("counter"),
            data.Attr("target"),
            data.Enum("operation", SessionCounterComparer.CounterOperation.Equal));
    }

    public override void Update() {
        base.Update();

        var level = FrostModule.TryGetCurrentLevel();
        if (level is null)
            return; // Shouldn't happen, but just in case

        var met = _comparer.Check(level);

        if (met && !_conditionMetLastFrame) {
            ActivateAll(Scene.Tracker.GetEntity<Player>());
        }

        _conditionMetLastFrame = met;
    }
}
