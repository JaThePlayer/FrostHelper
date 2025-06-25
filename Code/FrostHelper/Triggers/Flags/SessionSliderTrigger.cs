using FrostHelper.Helpers;

namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/SessionSliderTrigger")]
internal sealed class SessionSliderTrigger : Trigger {
    private readonly CounterOperation _operation;
    private readonly bool _clearOnSpawn;

    private readonly bool _once;

    private readonly SliderAccessor _slider;
    private readonly ConditionHelper.Condition _value;

    private enum CounterOperation {
        Increment,
        Decrement,
        Multiply,
        Divide,
        Remainder,
        Power,
        Set,
        
        Min,
        Max,
        Distance,
    }
    
    public SessionSliderTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        _slider = new(data.Attr("slider", ""));
        _value = data.GetCondition("value");
        
        _operation = data.Enum("operation", CounterOperation.Set);
        _clearOnSpawn = data.Bool("clearOnSpawn", false);
        _once = data.Bool("once");
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (_clearOnSpawn && scene is Level level) {
            _slider.Set(level.Session, 0);
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (FrostModule.TryGetCurrentLevel() is { } lvl) {
            var counter = _slider.GetObj(lvl.Session);
            var value = _value.GetFloat(lvl.Session);
            
            switch (_operation) {
                case CounterOperation.Increment:
                    counter.Value += value;
                    break;
                case CounterOperation.Decrement:
                    counter.Value -= value;
                    break;
                case CounterOperation.Multiply:
                    counter.Value *= value;
                    break;
                case CounterOperation.Divide:
                    counter.Value /= value;
                    break;
                case CounterOperation.Remainder:
                    counter.Value %= value;
                    break;
                case CounterOperation.Set:
                    counter.Value = value;
                    break;
                case CounterOperation.Max:
                    counter.Value = float.Max(counter.Value, value);
                    break;
                case CounterOperation.Min:
                    counter.Value = float.Min(counter.Value, value);
                    break;
                case CounterOperation.Distance:
                    counter.Value = float.Abs(counter.Value - value);
                    break;
                case CounterOperation.Power:
                    counter.Value = float.Pow(counter.Value, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_operation));
            }
            
            if (_once)
                RemoveSelf();
        }
    }
}