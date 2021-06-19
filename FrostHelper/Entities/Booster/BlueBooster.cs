using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper.Entities.Boosters
{
    [CustomEntity("FrostHelper/BlueBooster")]
    [Tracked]
    public class BlueBooster : GenericCustomBooster
    {
        public BlueBooster(EntityData data, Vector2 offset) : base(data, offset) { }

        public override void HandleBoostBegin(Player player)
        {
            Level level = player.SceneAs<Level>();
            bool doNotDropTheo = false;
            if (level != null)
            {
                MapMetaModeProperties meta = level.Session.MapData.GetMeta();
                doNotDropTheo = (meta != null) && meta.TheoInBubble.GetValueOrDefault();
            }
            //player.RefillDash();
            player.RefillStamina();
            if (doNotDropTheo)
            {
                return;
            }
            player.Drop();
        }
    }
}
