namespace FrostHelper;

[CustomEntity("FrostHelper/DoorDisableTrigger")]
public class DoorDisableTrigger : Trigger {
    public DoorDisableTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

    public override void OnEnter(Player player) {
        base.OnEnter(player);
        var pPos = player.Position;

        var door = Scene.Tracker.GetNearestEntity<Door>(pPos);
        var staticDoor = Scene.Tracker.GetNearestEntity<StaticDoor>(pPos);

        if (staticDoor is null) {
            door?.SetValue("disabled", true);
            return;
        }
        if (door is null) {
            staticDoor.Disabled = true;
            return;
        }

        // Disable the closest one
        if (Vector2.DistanceSquared(door.Position, pPos) > Vector2.DistanceSquared(staticDoor.Position, pPos)) {
            staticDoor.Disabled = true;
        } else {
            door.SetValue("disabled", true);
        }
    }
}
