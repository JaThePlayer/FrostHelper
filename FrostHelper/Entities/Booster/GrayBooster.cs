using Celeste;
using Celeste.Mod.Entities;
using Celeste.Mod.Meta;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper.Entities.Boosters
{
    [CustomEntity("FrostHelper/GrayBooster")]
    [Tracked]
    public class GrayBooster : GenericCustomBooster
    {
        public GrayBooster(EntityData data, Vector2 offset) : base(data, offset) 
        {
        }

        public override bool CanFastbubble() => false;
    }
}