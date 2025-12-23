using FrostHelper.Components;
using FrostHelper.Helpers;

namespace FrostHelper.Entities;

[CustomEntity("FrostHelper/ExpressionToCounterController")]
internal sealed class ExpressionToCounterController : Entity {
    private readonly CounterAccessor _counter;
    
    public ExpressionToCounterController(EntityData data, Vector2 offset) : base(data.Position + offset) {
        Active = true;
        _counter = new CounterAccessor(data.Attr("counter"));
        var value = data.GetCondition("expression");

        Add(new ExpressionListener<int>(value, OnConditionChanged, activateOnStart: true));
    }

    private void OnConditionChanged(Entity arg1, Maybe<int> old, int newValue) {
        if (Scene is Level l) {
            _counter.Set(l.Session, newValue);
        }
    }
}
