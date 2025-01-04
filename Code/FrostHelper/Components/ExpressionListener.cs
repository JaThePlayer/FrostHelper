using FrostHelper.Helpers;

namespace FrostHelper.Components;

internal sealed class ExpressionListener(ConditionHelper.Condition cond, Action<Entity, object?, object> onCondition, bool activateOnStart)
    : Component(true, false) {

    private object? _lastValue;
    
    public override void Update() {
        var value = cond.Get(FrostModule.GetCurrentLevel().Session, userdata: null);
        if (_lastValue is null) {
            if (activateOnStart) {
                onCondition(Entity, _lastValue, value);
            }
            _lastValue = value;
        }

        if (!value.Equals(_lastValue)) {
            onCondition(Entity, _lastValue, value);
            _lastValue = value;
        }
    }
}