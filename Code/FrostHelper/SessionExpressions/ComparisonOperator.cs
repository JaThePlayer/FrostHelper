using FrostHelper.Helpers;
using System.Numerics;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal interface IComparisonOperator {
    public static abstract bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool>;

    public static abstract bool Compare(string a, string b);

    public static abstract List<OpCode>? OpCodeSequence { get; }
}

internal struct OperatorEq : IComparisonOperator {
    public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
        return a == b;
    }

    public static bool Compare(string a, string b) {
        return a == b;
    }

    public static List<OpCode> OpCodeSequence { get; } = [OpCodes.Ceq];
}

internal struct OperatorNe : IComparisonOperator {
    public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
        return a != b;
    }

    public static bool Compare(string a, string b) {
        return a != b;
    }

    public static List<OpCode> OpCodeSequence { get; } = [
        OpCodes.Ceq, OpCodes.Ldc_I4_0, OpCodes.Ceq
    ];
}

internal struct OperatorGt : IComparisonOperator {
    public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
        return a > b;
    }

    public static bool Compare(string a, string b) {
        return a.CompareTo(b, StringComparison.InvariantCulture) > 0;
    }

    public static List<OpCode> OpCodeSequence { get; } = [OpCodes.Cgt];
}

internal struct OperatorLt : IComparisonOperator {
    public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
        return a < b;
    }

    public static bool Compare(string a, string b) {
        return a.CompareTo(b, StringComparison.InvariantCulture) < 0;
    }

    public static List<OpCode> OpCodeSequence { get; } = [OpCodes.Clt];
}

internal struct OperatorGte : IComparisonOperator {
    public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
        return a >= b;
    }

    public static bool Compare(string a, string b) {
        return a.CompareTo(b, StringComparison.InvariantCulture) >= 0;
    }

    public static List<OpCode> OpCodeSequence { get; } = [
        OpCodes.Clt, OpCodes.Ldc_I4_0, OpCodes.Ceq
    ];
}

internal struct OperatorLte : IComparisonOperator {
    public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
        return a <= b;
    }

    public static bool Compare(string a, string b) {
        return a.CompareTo(b, StringComparison.InvariantCulture) <= 0;
    }

    public static List<OpCode> OpCodeSequence { get; } = [
        OpCodes.Cgt, OpCodes.Ldc_I4_0, OpCodes.Ceq
    ];
}

internal sealed class ComparisonOperator<TOp>(ConditionHelper.Condition condA, ConditionHelper.Condition condB)
    : ConditionHelper.BinaryOperator(condA, condB) where TOp : IComparisonOperator {
    private static readonly MethodInfo Method_TOp_Compare_T_T = typeof(TOp)
        .GetMethod(nameof(TOp.Compare), 1, BindingFlags.Static | BindingFlags.Public, null,
            [Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0)], null)!;

    private static readonly MethodInfo Method_TOp_Compare_String_String = typeof(TOp)
        .GetMethod(nameof(TOp.Compare), 0, BindingFlags.Static | BindingFlags.Public, null,
            [typeof(string), typeof(string)], null)!;

    private static readonly MethodInfo Method_Dispatch = typeof(ComparisonOperator<TOp>)
        .GetMethod(nameof(Dispatch), 0, BindingFlags.Static | BindingFlags.NonPublic, null,
            [typeof(object), typeof(object)], null)!;


    private static bool Dispatch(object a, object b) {
        return (a, b) switch {
            (int ai, int bi) => TOp.Compare(ai, bi),
            (float ai, float bi) => TOp.Compare(ai, bi),
            (string ai, string bi) => TOp.Compare(ai, bi),
            _ => LogIncomparableTypes(a, b)
        };
    }

    protected override object Operate(object a, object b) {
        return Dispatch(a, b) ? One : Zero;
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        var aType = ConditionA.ReturnType;
        var bType = ConditionB.ReturnType;

        if (ConditionA.ReturnTypeIsNumber && ConditionB.ReturnTypeIsNumber) {
            var coercedType = aType == bType ? aType! : typeof(float);

            EmitGetValuesFromChildConditions(ctx, coercedType);
            if (TOp.OpCodeSequence is { } opCodes) {
                foreach (var opCode in opCodes) {
                    ctx.Il.Emit(opCode);
                }
            } else {
                ctx.Il.Emit(OpCodes.Call, Method_TOp_Compare_T_T.MakeGenericMethod(coercedType));
            }

            ctx.EmitConvertTo(typeof(bool), targetType);
            return;
        }

        if (aType == typeof(string) && bType == typeof(string)) {
            EmitGetValuesFromChildConditions(ctx, typeof(string));
            ctx.Il.Emit(OpCodes.Call, Method_TOp_Compare_String_String);
            ctx.EmitConvertTo(typeof(bool), targetType);
            return;
        }


        EmitGetValuesFromChildConditions(ctx, typeof(object));
        ctx.Il.Emit(OpCodes.Call, Method_Dispatch);
        ctx.EmitConvertTo(typeof(bool), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => InnerConditionsUseCurrentConditionLocalInEmit;

    private static bool LogIncomparableTypes(object a, object b) {
        NotificationHelper.Notify(
            $"Can't compare objects of types: {a.GetType()} and {b.GetType()}. Result will always be 0!");
        return false;
    }

    protected internal override Type ReturnType => typeof(int);
}