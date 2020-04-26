using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/SpeedBerryCollectTrigger")]
    [Tracked]
    class SpeedBerryCollectTrigger : Trigger
    {
        // Actual collection check is done in SpeedBerry.Update()
        public SpeedBerryCollectTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {

        }
    }
}
