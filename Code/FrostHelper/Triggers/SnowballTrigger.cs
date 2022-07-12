namespace FrostHelper;

[CustomEntity("FrostHelper/SnowballTrigger")]
public class SnowballTrigger : Trigger {
    public float Speed;
    public float ResetTime;
    public bool DrawOutline;
    public string SpritePath;
    public float SineWaveFrequency;
    public CustomSnowball.AppearDirection AppearDirection;
    public bool ReplaceExisting;

    public SnowballTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        SpritePath = data.Attr("spritePath", "snowball");
        Speed = data.Float("speed", 200f);
        ResetTime = data.Float("resetTime", 0.8f);
        SineWaveFrequency = data.Float("ySineWaveFrequency", 0.5f);
        DrawOutline = data.Bool("drawOutline", true);
        AppearDirection = data.Enum("direction", CustomSnowball.AppearDirection.Right);
        ReplaceExisting = data.Bool("replaceExisting", true);
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        CustomSnowball snowball;
        if (ReplaceExisting && (snowball = Scene.Entities.FindFirst<CustomSnowball>()) != null) {
            snowball.Speed = Speed;
            snowball.ResetTime = ResetTime;
            snowball.Sine.Frequency = SineWaveFrequency;
            if (snowball.Sprite.Path != SpritePath) {
                snowball.CreateSprite(SpritePath);
            }
            snowball.DrawOutline = DrawOutline;
            snowball.appearDirection = AppearDirection;
        } else {
            Scene.Add(new CustomSnowball(SpritePath, Speed, ResetTime, SineWaveFrequency, DrawOutline, AppearDirection));
        }
        RemoveSelf();
    }
}
