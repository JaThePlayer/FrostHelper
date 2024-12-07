using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnExpressionChangedActivator")]
internal sealed class OnExpressionChangedActivator : BaseActivator {
    public OnExpressionChangedActivator(EntityData data, Vector2 offset) : base(data, offset)
    {       
        Add(new ExpressionListener(data.GetCondition("expression"), OnExprChanged, activateOnStart: false));
        Active = true;
        Visible = false;
        Collidable = false;
    }

    private static void OnExprChanged(Entity self, object? prev, object curr) {
        ((OnExpressionChangedActivator)self).ActivateAll(self.Scene.Tracker.SafeGetEntity<Player>()!);
    }
}