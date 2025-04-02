namespace FrostHelper;

[CustomEntity("FrostHelper/RainbowFireBarrier")]
internal sealed class RainbowFireBarrier : CustomFireBarrier {
    public RainbowFireBarrier(EntityData data, Vector2 offset) : base(data, offset) {
        Lava.IsRainbow = CustomLavaRect.RainbowModes.All;
    }
}