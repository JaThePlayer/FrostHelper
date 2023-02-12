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
    }

    public void OnFlag(bool value) {
        if (value == TargetValue) {
            ActivateAll(Scene.Tracker.GetEntity<Player>());
        } else {
            CallOnLeave();
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        ActivateAll(player);
    }

    public override void Update() {
        CallOnStay();
    }
}
