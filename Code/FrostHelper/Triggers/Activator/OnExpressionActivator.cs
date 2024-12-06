using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Triggers.Activator;

[CustomEntity("FrostHelper/OnExpressionActivator")]
internal sealed class OnExpressionActivator : BaseActivator {
    public OnExpressionActivator(EntityData data, Vector2 offset) : base(data, offset) {
        Add(new ExpressionListener(data.GetCondition("expression"), 
            static e => (e as OnExpressionActivator)!.ActivateAll(e.Scene.Tracker.SafeGetEntity<Player>()!)));
        
        Active = true;
        Visible = false;
        Collidable = false;
    }
}