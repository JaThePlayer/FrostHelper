namespace FrostHelper;

[CustomEntity("FrostHelper/BloomPoint")]
public class BloomPointEntity : Entity {
    public BloomPointEntity(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Add(new BloomPoint(data.Float("alpha", 1f), data.Float("radius", 16f)));
    }
}
