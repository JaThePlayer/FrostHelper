using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper {
    // actual logic handled in CustomRisingLava.Awake()
    [Tracked]
    [CustomEntity("FrostHelper/CustomRisingLavaStartHeightTrigger")]
    public class CustomRisingLavaStartHeightTrigger : Trigger {
        public Vector2 Node;
        public CustomRisingLavaStartHeightTrigger(EntityData data, Vector2 offset) : base(data, offset) {
            Node = data.NodesOffset(offset)[0];
        }
    }
}
