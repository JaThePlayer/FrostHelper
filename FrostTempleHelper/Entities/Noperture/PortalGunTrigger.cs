using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.OutbackHelper;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostTempleHelper.Entities.azcplo1k
{
    [CustomEntity("noperture/portalgunTrigger")]
    class adyradz : Trigger
    {
        public adyradz(EntityData data, Vector2 offset) : base(data, offset)
        {
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            foreach (var item in Engine.Scene.Tracker.GetEntities<Portal>()) {
                abcdhr.UpdatePortal((Portal)item);
            }
            player.Add(new abcdhr());
        }
    }
}
