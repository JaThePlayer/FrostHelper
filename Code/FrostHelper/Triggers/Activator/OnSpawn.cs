namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Activates triggers on room load
/// </summary>
[CustomEntity("FrostHelper/OnSpawnActivator")]
internal sealed class OnSpawnActivator : BaseActivator {

    public OnSpawnActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;
    }

    public override void Awake(Scene scene) {
        base.Awake(scene);

        Activate(scene);
    }

    private void Activate(Scene scene) {
        ActivateAll(scene.Tracker.GetEntity<Player>());
        RemoveSelf();
    }
}
