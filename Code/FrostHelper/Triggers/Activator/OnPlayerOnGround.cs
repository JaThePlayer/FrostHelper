namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnPlayerOnGroundActivator")]
internal sealed class OnPlayerOnGround : BaseActivator {
    public bool OnlyWhenJustLanded;
    //public bool HasToBeInside;

    private bool wasActivated = false;

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

    public override void OnLeave(Player player) {
        base.OnLeave(player);

        //if (HasToBeInside)
            CallOnLeave(player);
    }

    public override void Update() {
        base.Update();

        //if (!HasToBeInside) {
        //    TryActivate(Scene.Tracker.GetEntity<Player>());
        //}
    }

    private void TryActivate(Player player) {
        if (player is null)
            return;

        if (player.onGround && (!OnlyWhenJustLanded || !player.wasOnGround)) {
            wasActivated = true;
            ActivateAll(player);
        } else if (wasActivated) {
            wasActivated = false;
            CallOnLeave(player);
        }
    }
}
