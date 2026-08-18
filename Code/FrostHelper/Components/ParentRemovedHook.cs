namespace FrostHelper.Components;

/// <summary>
/// Allows running a callback when the parent entity gets removed.
/// </summary>
internal sealed class ParentRemovedHook(Action<Entity> onRemoved) : Component(false, false) {
    public override void EntityRemoved(Scene scene) {
        onRemoved(Entity);
        base.EntityRemoved(scene);
    }
}
