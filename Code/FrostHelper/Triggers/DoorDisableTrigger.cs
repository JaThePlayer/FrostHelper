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
            if (door is { })
                door.disabled = true;
            return;
        }
        if (door is null) {
            staticDoor.Disable();
            return;
        }

        // Disable the closest one
        if (Vector2.DistanceSquared(door.Position, pPos) > Vector2.DistanceSquared(staticDoor.Position, pPos)) {
            staticDoor.Disable();
        } else {
            door.disabled = true;
        }
    }
}
