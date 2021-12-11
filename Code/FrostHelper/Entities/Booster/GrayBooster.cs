using Celeste.Mod.Entities;

namespace FrostHelper.Entities.Boosters {
    [CustomEntity("FrostHelper/GrayBooster")]
    [Tracked]
    public class GrayBooster : GenericCustomBooster {
        public GrayBooster(EntityData data, Vector2 offset) : base(data, offset) {
            // reparse this property to make sure that this defaults to 0f instead of 0.3f if the property doesn't exist
            BoostTime = data.Float("boostTime", 0f);
        }

        public override bool CanFastbubble() => false;
    }
}