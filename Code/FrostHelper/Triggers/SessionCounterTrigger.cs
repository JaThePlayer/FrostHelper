using FrostHelper.Helpers;

namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/SessionCounterTrigger")]
internal sealed class SessionCounterTrigger : Trigger {
    public readonly CounterOperation Operation;
    private readonly bool _clearOnSpawn;

    private readonly bool _once;

    private readonly CounterAccessor _counter;
    private readonly CounterExpression _value;

    public enum CounterOperation {
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
        
        BitwiseOr,
        BitwiseAnd,
        BitwiseXor,
        BitwiseShiftLeft,
        BitwiseShiftRight,
    }
    
    public SessionCounterTrigger(EntityData data, Vector2 offset) : base(data, offset) {
        _counter = new(data.Attr("counter", ""));
        _value = new(data.Attr("value", "0"));
        
        Operation = data.Enum("operation", CounterOperation.Set);
        _clearOnSpawn = data.Bool("clearOnSpawn", false);
        _once = data.Bool("once");
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (_clearOnSpawn && scene is Level level) {
            _counter.Set(level.Session, 0);
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (FrostModule.TryGetCurrentLevel() is { } lvl) {
            var counter = _counter.GetObj(lvl.Session);
            
            var value = _value.GetInt(lvl.Session);
            switch (Operation) {
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
                case CounterOperation.BitwiseOr:
                    counter.Value |= value;
                    break;
                case CounterOperation.BitwiseAnd:
                    counter.Value &= value;
                    break;
                case CounterOperation.BitwiseXor:
                    counter.Value ^= value;
                    break;
                case CounterOperation.BitwiseShiftLeft:
                    counter.Value <<= value;
                    break;
                case CounterOperation.BitwiseShiftRight:
                    counter.Value >>= value;
                    break;
                case CounterOperation.Set:
                    counter.Value = value;
                    break;
                case CounterOperation.Max:
                    counter.Value = int.Max(counter.Value, value);
                    break;
                case CounterOperation.Min:
                    counter.Value = int.Min(counter.Value, value);
                    break;
                case CounterOperation.Distance:
                    counter.Value = int.Abs(counter.Value - value);
                    break;
                case CounterOperation.Power:
                    counter.Value = (int)Math.Pow(counter.Value, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Operation));
            }
            
            if (_once)
                RemoveSelf();
        }
    }
}