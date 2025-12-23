using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnExpressionActivator")]
internal sealed class OnExpressionActivator : BaseActivator {
    public OnExpressionActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Add(new ExpressionListener<bool>(data.GetCondition("expression"), OnExprChanged, activateOnStart: true));
        
        Active = true;
        Visible = false;
        Collidable = false;
    }

    private static void OnExprChanged(Entity e, Maybe<bool> prev, bool next) {
        var prevB = prev.HasValue && prev.Value;
        var nextB = next;
        
        if (nextB && !prevB)
            (e as OnExpressionActivator)!.ActivateAll(e.Scene.Tracker.SafeGetEntity<Player>()!);
    }
}