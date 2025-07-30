using FrostHelper.Components;

namespace FrostHelper.Triggers.Activator;

/// <summary>
/// Activates triggers on room load
/// </summary>
[CustomEntity("FrostHelper/OnSpawnActivator")]
internal sealed class OnSpawnActivator : BaseActivator {
    private readonly bool _activateOnTransition;
    
    public OnSpawnActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Collidable = false;
        Add(new PostAwakeHook(Activate));
        _activateOnTransition = data.Bool("activateOnTransition", true);
    }

    private void Activate() {
        if (_activateOnTransition || Scene.ToLevel().LastIntroType != Player.IntroTypes.Transition) {
            ActivateAll(Scene.Tracker.GetEntity<Player>());
        }
        RemoveSelf();
    }
}
