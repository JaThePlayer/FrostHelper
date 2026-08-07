using FrostHelper.Helpers;

namespace FrostHelper.SessionExpressions;

internal interface IConstCondition;

internal interface IConstCondition<out T> : IConstCondition {
    T Value { get; }
}

internal sealed class ConstInt(int x) : ConditionHelper.Condition, IConstCondition<int>, IConstCondition<float> {
    private readonly object _boxed = x;

    public override object Get(Session session, object? userdata) => _boxed;

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        ctx.Il.EmitLoadConstAs(_boxed, targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => false;

    public override bool OnlyChecksFlags() => true;

    public int Value => x;

    protected internal override Type ReturnType => typeof(int);

    protected override IEnumerable<object> GetArgsForDebugPrint() => [_boxed];

    float IConstCondition<float>.Value => Value;
}

internal sealed class ConstFloat(float x) : ConditionHelper.Condition, IConstCondition<int>, IConstCondition<float> {
    private readonly object _boxed = x;

    public override object Get(Session session, object? userdata) => _boxed;

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        ctx.Il.EmitLoadConstAs(_boxed, targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => false;

    public override bool OnlyChecksFlags() => true;

    public float Value => x;

    protected internal override Type ReturnType => typeof(float);

    protected override IEnumerable<object> GetArgsForDebugPrint() => [x];

    int IConstCondition<int>.Value => (int) x;
}

internal sealed class ConstString(string x) : ConditionHelper.Condition, IConstCondition<string> {
    public string Value => x;

    public override object Get(Session session, object? userdata) => x;

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        ctx.Il.EmitLoadConstAs(x, targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => false;

    public override bool OnlyChecksFlags() => true;

    protected internal override Type ReturnType => typeof(string);

    protected override IEnumerable<object> GetArgsForDebugPrint() => [x];
}
