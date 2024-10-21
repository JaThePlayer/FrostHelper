using System.Globalization;
using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;

internal sealed class CounterExpression {
    private readonly int Value;
    private readonly string? ValueCounterName;
    private Session.Counter? _valueCounter;
    
    public CounterExpression(string expr) {
        if (!int.TryParse(expr, CultureInfo.InvariantCulture, out Value))
            ValueCounterName = expr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Get(Session session) {
        var value = ValueCounterName is { }
            ? (_valueCounter ??= session.GetCounterObj(ValueCounterName)).Value
            : Value;

        return value;
    }
}

internal sealed class CounterAccessor {
    private readonly string _counterName;
    private Session.Counter? _counter;
    
    internal enum CounterTimeUnits {
        Hours,
        Minutes,
        Seconds,
        Milliseconds,
    }
    
    public CounterAccessor(string counterName) {
        _counterName = counterName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(Session session, int value) {
        _counter ??= session.GetCounterObj(_counterName);

        _counter.Value = value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTime(Session session, TimeSpan time, CounterTimeUnits unit) {
        _counter ??= session.GetCounterObj(_counterName);

        _counter.Value = unit switch {
            CounterTimeUnits.Milliseconds => (int)time.TotalMilliseconds,
            CounterTimeUnits.Seconds => (int)time.TotalSeconds,
            CounterTimeUnits.Minutes => (int)time.TotalMinutes,
            CounterTimeUnits.Hours => (int)time.TotalHours,
            _ => throw new ArgumentOutOfRangeException(nameof(unit))
        };

        // On overflow, set the value to max instead of a negative value, so relative comparisons don't break.
        if (_counter.Value < 0)
            _counter.Value = int.MaxValue;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Get(Session session) {
        _counter ??= session.GetCounterObj(_counterName);

        return _counter.Value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Session.Counter GetObj(Session session) {
        return _counter ??= session.GetCounterObj(_counterName);
    }
}