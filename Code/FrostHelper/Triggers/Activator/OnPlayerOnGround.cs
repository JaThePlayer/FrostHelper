namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnPlayerOnGroundActivator")]
internal class OnPlayerOnGround : BaseActivator {
    public bool OnlyWhenJustLanded;
    //public bool HasToBeInside;

    public OnPlayerOnGround(EntityData data, Vector2 offset) : base(data, offset) {
        OnlyWhenJustLanded = data.Bool("onlyWhenJustLanded", true);
        // HasToBeInside + OnlyWhenJustLanded doesn't work
        // a) if it's done in update, then wasOnGround will always = onGround
        // b) if the trigger is resized, other activators will start activating this one
        //HasToBeInside = data.Bool("hasToBeInside", true);
    }

    public override void OnStay(Player player) {
        base.OnStay(player);

        //if (HasToBeInside)
            TryActivate(player);
    }

    public override void Update() {
        base.Update();

        //if (!HasToBeInside) {
        //    Console.WriteLine("H");
        //    TryActivate(Scene.Tracker.GetEntity<Player>());
        //}
    }

    private void TryActivate(Player player) {
        if (player is { } && player.onGround && (!OnlyWhenJustLanded || !player.wasOnGround)) {
            ActivateAll(player);
        }
    }
}
