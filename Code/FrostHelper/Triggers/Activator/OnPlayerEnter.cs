namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnPlayerEnterActivator", "FrostHelper/OnEnterActivator")]
internal class OnPlayerEnterActivator : BaseActivator {
    public OnPlayerEnterActivator(EntityData data, Vector2 offset) : base(data, offset) {
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        ActivateAll(player);
    }
}
