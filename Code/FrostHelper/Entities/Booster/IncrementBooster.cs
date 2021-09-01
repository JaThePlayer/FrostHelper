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

            // reparse the argument, with a different default value
            DashRecovery = data.Int("dashes", Red ? 2 : 1);
        }

        public override void HandleDashRefill(Player player)
        {
            if (dashCap == -1)
            {
                player.Dashes += DashRecovery;
            }
            else
            {
                player.Dashes = Math.Min(player.Dashes + DashRecovery, dashCap);
            }
        }
    }
}
