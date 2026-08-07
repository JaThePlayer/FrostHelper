using FrostHelper.Helpers;
using System.Reflection.Emit;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal sealed class CounterAccessorCondition(string name) : ConditionHelper.Condition {
    private Session.Counter? _valueCounter;
    private WeakReference<Session>? _lastSession;

    private static readonly MethodInfo MethodGetInt
        = typeof(CounterAccessorCondition).GetMethod(nameof(GetCached), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private int GetCached(Session session) {
        if ((_lastSession?.TryGetTarget(out var last) ?? false) && last != session) {
            _valueCounter = null;
            _lastSession = null;
        }

        _lastSession ??= new WeakReference<Session>(session);
        _valueCounter ??= session.GetCounterObj(name);

        return _valueCounter.Value;
    }

    public override object Get(Session session, object? userdata) {
        return GetCached(session);
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        ctx.EmitLoadCurrentCondition<CounterAccessorCondition>();
        ctx.EmitLoadSession();
        ctx.Il.Emit(OpCodes.Callvirt, MethodGetInt);
        ctx.EmitConvertTo(typeof(int), targetType);
    }

    protected internal override Type ReturnType => typeof(int);

    protected override IEnumerable<object> GetArgsForDebugPrint() => [name];
}

internal sealed class IndirectCounterAccessor(ConditionHelper.Condition nameCond) : ConditionHelper.Condition {
    private readonly ConditionHelper.Condition _nameCondition = nameCond;

    private static readonly FieldInfo FieldNameCondition =
        typeof(IndirectCounterAccessor).GetField(nameof(_nameCondition), BindingFlags.Instance | BindingFlags.NonPublic)
        !;

    private static readonly MethodInfo MethodSessionGetCounter =
        typeof(Session).GetMethod(nameof(Session.GetCounter), BindingFlags.Instance | BindingFlags.Public)!;

    public override object Get(Session session, object? userdata) {
        var name = _nameCondition.GetString(session, userdata);

        return session.GetCounter(name);
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        LocalBuilder? temp = null;
        ctx.EmitLoadSession();

        ctx.EmitSwapOutCurrentCondition(ref temp, _nameCondition, FieldNameCondition);
        _nameCondition.Emit(ctx, typeof(string));
        ctx.EmitRevertCurrentCondition(temp);

        ctx.Il.Emit(OpCodes.Callvirt, MethodSessionGetCounter);
        ctx.EmitConvertTo(typeof(int), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => _nameCondition.UsesCurrentConditionLocalInEmit;

    protected internal override Type ReturnType => typeof(int);

    protected override IEnumerable<object> GetArgsForDebugPrint() => [_nameCondition];
}
