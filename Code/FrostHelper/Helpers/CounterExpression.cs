using System.Globalization;
using System.Runtime.CompilerServices;

namespace FrostHelper.Helpers;

internal sealed class CounterExpression {
    private readonly int _value;
    private readonly CounterAccessor? _valueCounter;
    private readonly ConditionHelper.Condition? _valueCondition;
    
    public CounterExpression(string expr) {
        expr = expr.Trim();

        if (!int.TryParse(expr, CultureInfo.InvariantCulture, out _value)) {
            var parsed = ConditionHelper.CreateOrDefault(expr, "");
            if (parsed.IsSimpleFlagCheck(out _))
            {
                // If this is just a simple flag check,
                // then legacy support requires us to treat the name as a counter name, not as a flag!
                _valueCounter = new(expr);
            } else {
                _valueCondition = parsed;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInt(Session session, object? userdata = null) {
        return _valueCounter?.Get(session) ?? _valueCondition?.GetInt(session, userdata) ?? _value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetFloat(Session session, object? userdata = null) {
        return (float?)_valueCounter?.Get(session) ?? _valueCondition?.GetFloat(session, userdata) ?? _value;
    }

    public object GetObject(Session session, object? userdata = null) {
        return _valueCounter?.Get(session) ?? _valueCondition?.Get(session, userdata) ?? _value;
    }

    public ConditionHelper.Condition ToCondition() {
        if (_valueCondition is { })
            return _valueCondition;
        if (_valueCounter is { })
            return ConditionHelper.CreateOrDefault($"#{_valueCounter.CounterName}", "");

        return ConditionHelper.CreateOrDefault(_value.ToString(), "");
    }
}

internal sealed class CounterAccessor {
    private readonly string _counterName;
    private Session.Counter? _counter;

    public string CounterName => _counterName;
    
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