using System.Globalization;

namespace FrostHelper.Helpers;

/// <summary>
/// Helper class for comparing session counter values
/// </summary>
internal sealed class SessionCounterComparer {
    public readonly string CounterName;
    
    public readonly int Target;
    public readonly string? TargetCounterName;
    
    public readonly CounterOperation Operation;

    private Session.Counter? _counter;
    private Session.Counter? _targetCounter;

    public SessionCounterComparer(string counterName, string target, CounterOperation operation) {
        CounterName = counterName;
        if (!int.TryParse(target, CultureInfo.InvariantCulture, out Target))
            TargetCounterName = target;
        Operation = operation;
    }

    public bool Check(Level level) {
        _counter ??= level.Session.GetCounterObj(CounterName);

        var target = Target;
        if (TargetCounterName != null) {
            _targetCounter ??= level.Session.GetCounterObj(TargetCounterName);
            target = _targetCounter.Value;
        }
        
        var met = Operation switch {
            CounterOperation.Equal => _counter.Value == target,
            CounterOperation.NotEqual => _counter.Value != target,
            CounterOperation.GreaterThan => _counter.Value > target,
            CounterOperation.GreaterThanOrEqual => _counter.Value >= target,
            CounterOperation.LessThan => _counter.Value < target,
            CounterOperation.LessThanOrEqual => _counter.Value <= target,
            _ => throw new ArgumentOutOfRangeException()
        };

        return met;
    }
    
    public enum CounterOperation {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
    }
}