using System;
using Celeste;
using Microsoft.Xna.Framework;
using Celeste.Mod.Entities;

namespace FrostHelper.Triggers
{
    [CustomEntity("FrostHelper/CapDashOnGroundTrigger")]
    public class CapDashOnGroundTrigger : Trigger
    {
        public CapDashOnGroundTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

        public override void OnStay(Player player)
        {
            base.OnStay(player);
            if (player.OnGround())
            {
                player.Dashes = Math.Min(player.Dashes, player.MaxDashes);
            }
        }
    }
}
