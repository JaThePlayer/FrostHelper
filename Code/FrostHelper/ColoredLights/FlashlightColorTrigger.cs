// Because this used to be an entity from Colored Lights, make sure the C# type name for old entities stays correct,
// just in case some map relies on it. For new maps, only the Frost Helper placement is available, and those will use
// the type in the FrostHelper namespace, to avoid confusion in places that expose type names to the user.
namespace ColoredLights {
    [CustomEntity("coloredlights/flashlightColorTrigger")]
    internal sealed class FlashlightColorTrigger : FrostHelper.FlashlightColorTrigger {
        public FlashlightColorTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
        }
    }
}

namespace FrostHelper {
    [CustomEntity("FrostHelper/FlashlightColorTrigger")]
    internal class FlashlightColorTrigger : Trigger
    {
        private readonly Color _color;
        private readonly float _timer;
        private readonly bool _persistent;
        private Color _prevColor;
    
        public FlashlightColorTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            _color = data.GetColor("color", "ffffff");
            _timer = data.Float("time", -1f);
            _persistent = data.Bool("persistent", true);
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
        
            _prevColor = player.Light.Color;
            if (_persistent)
                FrostModule.Session.FlashlightColor = _color;
            player.Light.Color = _color;
            if (_timer != -1f)
            {
                Add(new Coroutine(DelayedResetFlashlightColor(player)));
            }
        }

        public IEnumerator DelayedResetFlashlightColor(Player player)
        {
            yield return _timer;
            if (player.Light.Color == _color)
            {
                if (_persistent)
                    FrostModule.Session.FlashlightColor = _prevColor;
                player.Light.Color = _prevColor;
            }   
        }
    }
}