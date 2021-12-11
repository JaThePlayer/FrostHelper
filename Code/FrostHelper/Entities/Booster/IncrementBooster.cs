using Celeste.Mod.Entities;

namespace FrostHelper.Entities.Boosters {
    [CustomEntity("FrostHelper/IncrementBooster")]
    [Tracked]
    public class IncrementBooster : GenericCustomBooster {
        public int DashCap;
        public bool RefillBeforeIncrementing;

        public IncrementBooster(EntityData data, Vector2 offset) : base(data, offset) {
            DashCap = data.Int("dashCap", -1);
            // mainly for backwards compatibility
            RefillBeforeIncrementing = data.Bool("refillBeforeIncrementing", false);

            // reparse the argument, with a different default value
            DashRecovery = data.Int("dashes", Red ? 2 : 1);
        }

        public override void HandleDashRefill(Player player) {
            if (RefillBeforeIncrementing) {
                player.RefillDash();
            }

            if (DashCap == -1) {
                player.Dashes += DashRecovery;
            } else {
                player.Dashes = Math.Min(player.Dashes + DashRecovery, DashCap);
            }
        }
    }
}
