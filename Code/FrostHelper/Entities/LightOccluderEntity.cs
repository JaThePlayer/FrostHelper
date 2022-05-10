namespace FrostHelper;

[CustomEntity("FrostHelper/LightOccluderEntity")]
public class LightOccluderEntity : Entity {
    public LightOccluderEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Add(new LightOcclude(new(0, 0, data.Width, data.Height), data.Float("alpha", 1f)));
    }
}
