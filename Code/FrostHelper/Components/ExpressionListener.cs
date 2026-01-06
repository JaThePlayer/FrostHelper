using FrostHelper.Helpers;

namespace FrostHelper.Components;

internal sealed class ExpressionListener<T>(ConditionHelper.Condition cond, Action<Entity, Maybe<T>, T> onCondition, bool activateOnStart)
    : Component(true, false) where T : notnull {

    private Maybe<T> _lastValue;
    
    public override void Update() {
        var value = cond.Get<T>(Scene.ToLevel().Session, userdata: null);
        if (!_lastValue.HasValue) {
            if (activateOnStart) {
                onCondition(Entity, _lastValue, value);
            }
            _lastValue = value;
        }

        if (value is IEquatable<T> eq) {
            if (!eq.Equals(_lastValue.Value)) {
                onCondition(Entity, _lastValue, value);
                _lastValue = value;
            }

            return;
        }
        
        if (!value.Equals(_lastValue.Value)) {
            onCondition(Entity, _lastValue, value);
            _lastValue = value;
        }
    }
}