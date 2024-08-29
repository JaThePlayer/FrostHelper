namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Activates if the player is dashing while in the trigger
/// </summary>
[CustomEntity("FrostHelper/OnPlayerDashingActivator")]
internal sealed class OnPlayerDashing : BaseActivator {
    public bool OnlyWhenJustDashed;
    public readonly bool HasToBeInside;

    public OnPlayerDashing(EntityData data, Vector2 offset) : base(data, offset) {
        OnlyWhenJustDashed = data.Bool("onlyWhenJustDashed", true);
        HasToBeInside = data.Bool("hasToBeInside", true);
        if (OnlyWhenJustDashed || HasToBeInside) {
            Add(new DashListener((v) => {
                if (HasToBeInside && !PlayerIsInside) {
                    return;
                }
                ActivateAll(Scene.Tracker.GetEntity<Player>());
            }));
        }

        Active = !HasToBeInside && !OnlyWhenJustDashed;
        Collidable = HasToBeInside;
    }

    public override void Update() {
        base.Update();

        // edge-case where you want to fire an event each frame a player is dash attacking while not caring about the player being inside the trigger
        if (!HasToBeInside && !OnlyWhenJustDashed && Scene.Tracker.GetEntity<Player>() is { } player && player.DashAttacking) {
            ActivateAll(player);
        }
    }

    public override void OnStay(Player player) {
        base.OnStay(player);

        // When you want to fire an event each frame a player is dash attacking
        if (HasToBeInside && player.DashAttacking)
            ActivateAll(player);
    }
}
