namespace FrostHelper;

[CustomEntity("FrostHelper/StopCustomSnowballTrigger")]
internal sealed class StopCustomSnowballTrigger(EntityData data, Vector2 offset) : Trigger(data, offset) {
    private readonly bool _once = data.Bool("once");
    
    public override void OnEnter(Player player) {
        base.OnEnter(player);

        var any = false;
        foreach (CustomSnowball snowball in Scene.Tracker.SafeGetEntities<CustomSnowball>()) {
            snowball.StartLeaving();
            any = true;
        }

        if (any && _once) {
            RemoveSelf();
        }
    }
}
