namespace FrostHelper.Entities.Boosters;

[CustomEntity("FrostHelper/BlueBooster")]
[Tracked]
public class BlueBooster : GenericCustomBooster {
    public BlueBooster(EntityData data, Vector2 offset) : base(data, offset) { }

    public override void HandleDashRefill(Player player) {
        // no-op 
    }
}
