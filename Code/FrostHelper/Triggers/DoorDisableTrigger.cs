namespace FrostHelper;

[CustomEntity("FrostHelper/DoorDisableTrigger")]
public class DoorDisableTrigger : Trigger {
    public DoorDisableTrigger(EntityData data, Vector2 offset) : base(data, offset) { }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        var door = Scene.Tracker.GetNearestEntity<Door>(player.Position);

        door.SetValue("disabled", true);
    }
}
