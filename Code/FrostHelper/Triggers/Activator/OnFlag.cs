namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnFlagActivator")]
internal class OnFlagActivator : BaseActivator {
    public readonly bool TargetValue;

    public OnFlagActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;

        Add(new FlagListener(data.Attr("flag"), OnFlag, data.Bool("mustChange", false)));
        TargetValue = data.Bool("targetState", true);
    }

    public void OnFlag(bool value) {
        if (value == TargetValue) {
            ActivateAll(Scene.Tracker.GetEntity<Player>());
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        ActivateAll(player);
    }
}
