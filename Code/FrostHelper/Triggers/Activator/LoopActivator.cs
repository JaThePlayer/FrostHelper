using static FrostHelper.Helpers.ConditionHelper;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/LoopActivator")]
internal class LoopActivator : BaseActivator {
    public Condition Condition;

    // Whether this activator needs to be activated by some other source before starting the loop.
    public bool RequireActivation;

    public float LoopTime;

    public LoopActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Condition = data.GetCondition("condition");
        RequireActivation = data.Bool("requireActivation");
        LoopTime = data.Float("loopTime");

        if (!RequireActivation)
            Add(new Coroutine(LoopingRoutine()));
        Collidable = false;
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (RequireActivation)
            Add(new Coroutine(LoopingRoutine()));
    }

    public IEnumerator LoopingRoutine() {
        var player = Scene.Tracker.GetEntity<Player>();

        if (Delay != 0f)
            yield return Delay;

        while (player is { Scene: { } }) {
            if (Condition.Check()) {
                InstantActivateAll(player);
            }

            yield return LoopTime;
        }
    }
}
