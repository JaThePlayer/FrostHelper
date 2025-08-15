namespace FrostHelper;

[CustomEntity("FrostHelper/SnowballTrigger")]
internal sealed class SnowballTrigger(EntityData data, Vector2 offset) : Trigger(data, offset) {
    private readonly float _speed = data.Float("speed", 200f);
    private readonly float _resetTime = data.Float("resetTime", 0.8f);
    private readonly bool _drawOutline = data.Bool("drawOutline", true);
    private readonly string _spritePath = data.Attr("spritePath", "snowball");
    private readonly float _sineWaveFrequency = data.Float("ySineWaveFrequency", 0.5f);
    private readonly CustomSnowball.AppearDirection _appearDirection = data.Enum("direction", CustomSnowball.AppearDirection.Right);
    private readonly bool _replaceExisting = data.Bool("replaceExisting", true);
    private readonly float _safeZoneSize = data.Float("safeZoneSize", 64f);
    private readonly float _offset = data.Float("offset", 0f);
    private readonly bool _once = data.Bool("once", true);

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        if (_replaceExisting && Scene.Tracker.SafeGetEntity<CustomSnowball>() is {} snowball) {
            snowball.Speed = _speed;
            snowball.ResetTime = _resetTime;
            snowball.Sine.Frequency = _sineWaveFrequency;
            if (snowball.Sprite.Path != _spritePath) {
                snowball.CreateSprite(_spritePath);
            }
            snowball.DrawOutline = _drawOutline;
            snowball.appearDirection = _appearDirection;
            snowball.SafeZoneSize = _safeZoneSize;
        } else {
            Scene.Add(new CustomSnowball(_spritePath, _speed, _resetTime, _sineWaveFrequency, _drawOutline, _appearDirection, _safeZoneSize, _offset));
        }
        
        if (_once)
            RemoveSelf();
    }
}
