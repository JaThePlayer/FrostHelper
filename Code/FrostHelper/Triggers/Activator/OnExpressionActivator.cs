using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnExpressionActivator")]
internal sealed class OnExpressionActivator : BaseActivator {
    public OnExpressionActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Add(new ExpressionListener(data.GetCondition("expression"), OnExprChanged, activateOnStart: true));
        
        Active = true;
        Visible = false;
        Collidable = false;
    }

    private static void OnExprChanged(Entity e, object? prev, object next) {
        var prevB = prev is not null && ConditionHelper.Condition.CoerceToBool(prev);
        var nextB = ConditionHelper.Condition.CoerceToBool(next);
        
        if (nextB && !prevB)
            (e as OnExpressionActivator)!.ActivateAll(e.Scene.Tracker.SafeGetEntity<Player>()!);
    }
}