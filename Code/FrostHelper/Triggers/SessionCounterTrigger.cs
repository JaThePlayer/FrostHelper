using System.Globalization;

namespace FrostHelper.Triggers;

[CustomEntity("FrostHelper/SessionCounterTrigger")]
internal sealed class SessionCounterTrigger : Trigger {
    public readonly string CounterName;
    
    public readonly int Value;
    public readonly string? ValueCounterName;
    
    public readonly CounterOperation Operation;
    public readonly bool ClearOnSpawn;

    private Session.Counter? _counter;
    private Session.Counter? _valueCounter;

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
        CounterName = data.Attr("counter", "");
        var valueStr = data.Attr("value", "");
        if (!int.TryParse(valueStr, CultureInfo.InvariantCulture, out Value))
            ValueCounterName = valueStr;
        
        Operation = data.Enum("operation", CounterOperation.Set);
        ClearOnSpawn = data.Bool("clearOnSpawn", false);
    }

    public override void Added(Scene scene) {
        base.Added(scene);
        if (ClearOnSpawn && scene is Level level) {
            level.Session.SetCounter(CounterName, 0);
        }
    }

    public override void OnEnter(Player player) {
        base.OnEnter(player);

        if (FrostModule.TryGetCurrentLevel() is { } lvl) {
            _counter ??= lvl.Session.GetCounterObj(CounterName);
            var value = ValueCounterName is { }
                ? (_valueCounter ??= lvl.Session.GetCounterObj(ValueCounterName)).Value
                : Value;
            switch (Operation) {
                case CounterOperation.Increment:
                    _counter.Value += value;
                    break;
                case CounterOperation.Decrement:
                    _counter.Value -= value;
                    break;
                case CounterOperation.Multiply:
                    _counter.Value *= value;
                    break;
                case CounterOperation.Divide:
                    _counter.Value /= value;
                    break;
                case CounterOperation.Remainder:
                    _counter.Value %= value;
                    break;
                case CounterOperation.BitwiseOr:
                    _counter.Value |= value;
                    break;
                case CounterOperation.BitwiseAnd:
                    _counter.Value &= value;
                    break;
                case CounterOperation.BitwiseXor:
                    _counter.Value ^= value;
                    break;
                case CounterOperation.BitwiseShiftLeft:
                    _counter.Value <<= value;
                    break;
                case CounterOperation.BitwiseShiftRight:
                    _counter.Value >>= value;
                    break;
                case CounterOperation.Set:
                    _counter.Value = value;
                    break;
                case CounterOperation.Max:
                    _counter.Value = int.Max(_counter.Value, value);
                    break;
                case CounterOperation.Min:
                    _counter.Value = int.Min(_counter.Value, value);
                    break;
                case CounterOperation.Distance:
                    _counter.Value = int.Abs(_counter.Value - value);
                    break;
                case CounterOperation.Power:
                    _counter.Value = (int)Math.Pow(_counter.Value, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(Operation));
            }
        }
    }
}