namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnEnterActivator")]
internal class OnEnterActivator : BaseActivator {
    public OnEnterActivator(EntityData data, Vector2 offset) : base(data, offset) {
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        ActivateAll(player);
    }
}
