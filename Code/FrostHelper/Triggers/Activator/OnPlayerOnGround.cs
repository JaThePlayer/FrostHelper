namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnPlayerOnGroundActivator")]
internal class OnPlayerOnGround : BaseActivator {
    public bool OnlyWhenJustLanded;

    public OnPlayerOnGround(EntityData data, Vector2 offset) : base(data, offset) {
        OnlyWhenJustLanded = data.Bool("onlyWhenJustLanded", true);
    }

    public override void OnStay(Player player) {
        base.OnStay(player);

        TryActivate(player);
    }

    private void TryActivate(Player player) {
        if (player.onGround && (!OnlyWhenJustLanded || !player.wasOnGround)) {
            ActivateAll(player);
        }
    }
}
