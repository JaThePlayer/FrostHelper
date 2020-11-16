using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace FrostHelper
{
    [CustomEntity("FrostHelper/SnowballTrigger")]
    public class SnowballTrigger : Trigger
    {
        public float Speed;
        public float ResetTime;
        public bool DrawOutline;
        public string SpritePath;
        public float SineWaveFrequency;


        public SnowballTrigger(EntityData data, Vector2 offset) : base(data, offset)
        {
            SpritePath = data.Attr("spritePath", "snowball");
            Speed = data.Float("speed", 200f);
            ResetTime = data.Float("resetTime", 0.8f);
            SineWaveFrequency = data.Float("ySineWaveFrequency", 0.5f);
            DrawOutline = data.Bool("drawOutline");
        }
        
        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            CustomSnowball snowball;
            if ((snowball = Scene.Entities.FindFirst<CustomSnowball>()) == null)
            {
                Scene.Add(new CustomSnowball(SpritePath, Speed, ResetTime, SineWaveFrequency, DrawOutline));
            } else
            {
                snowball.Speed = Speed;
                snowball.ResetTime = ResetTime;
                snowball.Sine.Frequency = SineWaveFrequency;
                if (snowball.Sprite.Path != SpritePath)
                {
                    snowball.CreateSprite(SpritePath);
                }
                snowball.DrawOutline = DrawOutline;
            }
            RemoveSelf();
        }
    }
}
