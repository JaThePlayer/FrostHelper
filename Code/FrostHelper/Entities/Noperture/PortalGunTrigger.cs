#if PORTALGUN

using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.OutbackHelper;
using Microsoft.Xna.Framework;
using Monocle;

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
#endif