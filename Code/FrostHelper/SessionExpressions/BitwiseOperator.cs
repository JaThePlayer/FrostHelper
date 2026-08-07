using FrostHelper.Helpers;
using System.Numerics;
using System.Reflection.Emit;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal sealed class OperatorAnd(ConditionHelper.Condition a, ConditionHelper.Condition b)
    : ConditionHelper.Condition {
    private readonly ConditionHelper.Condition _a = a, _b = b;

    private static readonly FieldInfo
        FieldA = typeof(OperatorAnd).GetField(nameof(_a), BindingFlags.Instance | BindingFlags.NonPublic)!,
        FieldB = typeof(OperatorAnd).GetField(nameof(_b), BindingFlags.Instance | BindingFlags.NonPublic)!;

    public override object Get(Session session, object? userdata) {
        return CoerceToBool(_a.Get(session, userdata)) && CoerceToBool(_b.Get(session, userdata)) ? 1 : 0;
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        LocalBuilder? temp = null;
        var il = ctx.Il;
        var falseLabel = il.DefineLabel();
        var endLabel = il.DefineLabel();

        il.EmitSwapOutCurrentCondition(ref temp, ctx, _a, FieldA);
        _a.Emit(ctx, typeof(bool));
        il.Emit(OpCodes.Brfalse, falseLabel);

        il.EmitSwapOutCurrentCondition(ref temp, ctx, _b, FieldB);
        _b.Emit(ctx, typeof(bool));
        il.Emit(OpCodes.Br, endLabel);

        il.MarkLabel(falseLabel);
        il.Emit(OpCodes.Ldc_I4_0);

        il.MarkLabel(endLabel);

        il.EmitRevertCurrentCondition(temp, ctx);

        ctx.EmitConvertTo(typeof(bool), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit =>
        _a.UsesCurrentConditionLocalInEmit || _b.UsesCurrentConditionLocalInEmit;

    public override bool OnlyChecksFlags() => _a.OnlyChecksFlags() && _b.OnlyChecksFlags();

    protected internal override Type ReturnType => typeof(int);

    protected override IEnumerable<object> GetArgsForDebugPrint() => [_a, _b];
}

internal sealed class OperatorOr(ConditionHelper.Condition a, ConditionHelper.Condition b) : ConditionHelper.Condition {
    private readonly ConditionHelper.Condition _a = a, _b = b;

    private static readonly FieldInfo
        FieldA = typeof(OperatorOr).GetField(nameof(_a), BindingFlags.Instance | BindingFlags.NonPublic)!,
        FieldB = typeof(OperatorOr).GetField(nameof(_b), BindingFlags.Instance | BindingFlags.NonPublic)!;

    public override object Get(Session session, object? userdata) {
        return CoerceToBool(_a.Get(session, userdata)) || CoerceToBool(_b.Get(session, userdata)) ? One : Zero;
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        LocalBuilder? temp = null;
        var il = ctx.Il;
        var endLabel = il.DefineLabel();
        var trueLabel = il.DefineLabel();

        il.EmitSwapOutCurrentCondition(ref temp, ctx, _a, FieldA);
        _a.Emit(ctx, typeof(bool));
        il.Emit(OpCodes.Brtrue, trueLabel);

        il.EmitSwapOutCurrentCondition(ref temp, ctx, _b, FieldB);
        _b.Emit(ctx, typeof(bool));
        il.Emit(OpCodes.Br, endLabel);

        il.MarkLabel(trueLabel);
        il.Emit(OpCodes.Ldc_I4_1);

        il.MarkLabel(endLabel);

        il.EmitRevertCurrentCondition(temp, ctx);

        ctx.EmitConvertTo(typeof(bool), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit =>
        _a.UsesCurrentConditionLocalInEmit || _b.UsesCurrentConditionLocalInEmit;

    public override bool OnlyChecksFlags() => _a.OnlyChecksFlags() && _b.OnlyChecksFlags();

    protected internal override Type ReturnType => typeof(int);

    protected override IEnumerable<object> GetArgsForDebugPrint() => [_a, _b];
}

internal struct OperatorBitwiseOr : IBitwiseOperator {
    public static T Perform<T>(T a, T b) where T : IBinaryNumber<T> {
        return a | b;
    }

    public static OpCode? OpCode => OpCodes.Or;
}

internal struct OperatorBitwiseAnd : IBitwiseOperator {
    public static T Perform<T>(T a, T b) where T : IBinaryNumber<T> {
        return a & b;
    }

    public static OpCode? OpCode => OpCodes.And;
}

internal interface IBitwiseOperator {
    public static abstract T Perform<T>(T a, T b) where T : IBinaryNumber<T>;

    public static abstract OpCode? OpCode { get; }
}

internal sealed class BitwiseOperator<TOp>(ConditionHelper.Condition condA, ConditionHelper.Condition condB) : ConditionHelper.BinaryOperator(condA, condB) where TOp : IBitwiseOperator {
        private static readonly MethodInfo MethodPerformInt
            = typeof(TOp).GetMethod(nameof(IBitwiseOperator.Perform), BindingFlags.Static | BindingFlags.Public)!.MakeGenericMethod(typeof(int));
        
        protected override object Operate(object a, object b) {
            return (a, b) switch {
                (int aInt, int bInt) => TOp.Perform(aInt, bInt),
                (float aF, float bF) => TOp.Perform((int) aF, (int) bF),
                _ => LogIncomparableTypes(a, b)
            };
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            if (ConditionA.ReturnTypeIsNumber && ConditionB.ReturnTypeIsNumber) {
                EmitGetValuesFromChildConditions(ctx, typeof(int));
                if (TOp.OpCode is { } opCode) {
                    ctx.Il.Emit(opCode);
                } else {
                    ctx.Il.Emit(OpCodes.Call, MethodPerformInt);
                }
                ctx.EmitConvertTo(typeof(int), targetType);
            } else {
                base.Emit(ctx, targetType);
            }
        }

        internal override bool UsesCurrentConditionLocalInEmit 
            => InnerConditionsUseCurrentConditionLocalInEmit
               || !ConditionA.ReturnTypeIsNumber
               || !ConditionB.ReturnTypeIsNumber;

        private object LogIncomparableTypes(object a, object b) {
            NotificationHelper.Notify($"Can't perform bitwise operations on objects of types: {a.GetType()} and {b.GetType()}. Result will always be 0!");
            return 0;
        }
    }