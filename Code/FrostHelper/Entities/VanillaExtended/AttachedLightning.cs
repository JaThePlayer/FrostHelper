namespace FrostHelper;

[CustomEntity("FrostHelper/AttachedLightning")]
public class AttachedLightning : Lightning {
    public AttachedLightning(EntityData data, Vector2 offset) : base(data, offset) {
        Add(new GroupedStaticMover(data.Int("attachGroup", 0)) {
            OnShake = (amt) => Position += amt,
            SolidChecker = (solid) => CollideCheck(solid),
        });
    }
}
