namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Activates triggers when a flag changes, using flag listeners for performance.
/// </summary>
[CustomEntity("FrostHelper/OnFlagActivator")]
internal sealed class OnFlagActivator : BaseActivator {
    public readonly bool TargetValue;

    public OnFlagActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;

        Add(new FlagListener(data.Attr("flag"), OnFlag, data.Bool("mustChange", false), data.Bool("triggerOnRoomBegin", false)));
        TargetValue = data.Bool("targetState", true);

        // reparse with different default value to replicate old behavior
        ActivateAfterDeath = data.Bool("activateAfterDeath", true);
    }

    public override void Awake(Scene scene) {
        // OnFlag might get called during Awake's EntityAwake() calling loop,
        // causing a crash if a delay is set on the activator set to trigger on room begin (creating a Coroutine component)
        Components.LockMode = ComponentList.LockModes.Locked;
        base.Awake(scene);
        Components.LockMode = ComponentList.LockModes.Open;
    }

    private void OnFlag(Session session, string? flag, bool value) {
        if (value == TargetValue) {
            var player = Scene?.Tracker.GetEntity<Player>();
            if (player != null || ActivateAfterDeath)
                ActivateAll(player!);
        } else {
            CallOnLeave();
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        ActivateAll(player);
    }

    public override void Update() {
        base.Update();

        CallOnStay();
    }
}
