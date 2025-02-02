using FrostHelper.Components;

namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Activates triggers on room load
/// </summary>
[CustomEntity("FrostHelper/OnSpawnActivator")]
internal sealed class OnSpawnActivator : BaseActivator {
    public OnSpawnActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;
        Add(new PostAwakeHook(Activate));
    }

    private void Activate() {
        ActivateAll(Scene.Tracker.GetEntity<Player>());
        RemoveSelf();
    }
}
