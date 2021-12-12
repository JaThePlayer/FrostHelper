using Celeste.Mod.Entities;

namespace FrostHelper.Entities.Boosters {
    [CustomEntity("FrostHelper/YellowBooster")]
    [Tracked]
    public class YellowBooster : GenericCustomBooster {
        public Color FlashTint;

        public YellowBooster(EntityData data, Vector2 offset) : base(data, offset) {
            FlashTint = ColorHelper.GetColor(data.Attr("flashTint", "Red"));
        }

        public override IEnumerator HandleBoostCoroutine(Player player) {
            yield return BoostTime / 6;

            sprite.SetColor(FlashTint);
            yield return BoostTime / 3;

            sprite.SetColor(Color.White);
            yield return BoostTime / 6;

            sprite.SetColor(FlashTint);
            yield return BoostTime / 3;

            sprite.SetColor(Color.White);
            // Player didn't dash out, time to kill them :(
            player.Die(player.DashDir);
            PlayerDied();
            yield break;
        }

        public override void OnBoostEnd(Player player) {
            base.OnBoostEnd(player);
            sprite.SetColor(Color.White);
        }
    }
}