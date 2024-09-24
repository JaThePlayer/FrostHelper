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
    private readonly string CounterName;
    private Session.Counter? Counter;
    
    public CounterAccessor(string counterName) {
        CounterName = counterName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(Session session, int value) {
        Counter ??= session.GetCounterObj(CounterName);

        Counter.Value = value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Get(Session session) {
        Counter ??= session.GetCounterObj(CounterName);

        return Counter.Value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Session.Counter GetObj(Session session) {
        return Counter ??= session.GetCounterObj(CounterName);
    }
}