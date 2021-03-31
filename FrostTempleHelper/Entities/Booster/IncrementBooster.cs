using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace FrostHelper.Entities.Boosters
{
    [CustomEntity("FrostHelper/IncrementBooster")]
    [Tracked]
    public class IncrementBooster : GenericCustomBooster
    {
        public int dashCap;

        public IncrementBooster(EntityData data, Vector2 offset) : base(data, offset) 
        {
            dashCap = data.Int("dashCap", -1);
        }

        public override void Boost(Player player)
        {
            base.Boost(player);
            if (dashCap == -1)
            {
                player.Dashes += GetDashIncrementAmt();
            }
            else
            {
                player.Dashes = Math.Min(player.Dashes + GetDashIncrementAmt(), dashCap);
            }
        }

        private int GetDashIncrementAmt() => (Red ? 2 : 1);
    }
}
