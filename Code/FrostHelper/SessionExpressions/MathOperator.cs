using FrostHelper.Helpers;
using System.Diagnostics;
using System.Numerics;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace FrostHelper.SessionExpressions;

internal interface IMathOperator {
    static abstract T Perform<T>(T a, T b) where T : INumber<T>;

    static abstract Vector2 Perform(float a, Vector2 b);

    static abstract Vector2 Perform(Vector2 a, float b);

    static abstract Vector2 Perform(Vector2 a, Vector2 b);

    static abstract OpCode? PerformOpCode { get; }

    static abstract bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b);
}

internal sealed class MathOperator<TOp>(ConditionHelper.Condition condA, ConditionHelper.Condition condB) 
    : ConditionHelper.BinaryOperator(condA, condB) where TOp : IMathOperator {
    private static readonly MethodInfo Method_TOp_Perform_T_T = typeof(TOp)
            .GetMethod(nameof(TOp.Perform), 1, BindingFlags.Static | BindingFlags.Public, null, [ Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) ], null)!;

    private static readonly MethodInfo Method_TOp_Perform_float_Vector2 = typeof(TOp)
            .GetMethod(nameof(TOp.Perform), 0, BindingFlags.Static | BindingFlags.Public, null, [ typeof(float), typeof(Vector2) ], null)!;

    private static readonly MethodInfo Method_TOp_Perform_Vector2_float = typeof(TOp)
        .GetMethod(nameof(TOp.Perform), 0, BindingFlags.Static | BindingFlags.Public, null,
            [typeof(Vector2), typeof(float)], null)!;

    private static readonly MethodInfo Method_TOp_Perform_Vector2_Vector2 = typeof(TOp)
        .GetMethod(nameof(TOp.Perform), 0, BindingFlags.Static | BindingFlags.Public, null,
            [typeof(Vector2), typeof(Vector2)], null)!;


    private static readonly MethodInfo Method_Dispatch = typeof(MathOperator<TOp>)
        .GetMethod(nameof(Dispatch), BindingFlags.Static | BindingFlags.NonPublic, [typeof(object), typeof(object)])!;

    protected static object Dispatch(object a, object b) {
        return (a, b) switch {
            (int ai, int bi) => TOp.Perform(ai, bi),
            (int ai, float bi) => TOp.Perform(ai, bi),
            (float ai, float bi) => TOp.Perform(ai, bi),
            (float ai, int bi) => TOp.Perform(ai, bi),
            (float bi, Vector2 v2) => TOp.Perform(bi, v2),
            (int bi, Vector2 v2) => TOp.Perform(bi, v2),
            (Vector2 v2, int bi) => TOp.Perform(v2, bi),
            (Vector2 v2, float bi) => TOp.Perform(v2, bi),
            (Vector2 v2, Vector2 bi) => TOp.Perform(v2, bi),
            _ => LogIncomparableTypes(a, b)
        };
    }

    protected override object Operate(object a, object b) {
        return Dispatch(a, b);
    }

    private bool CanUseOpcode(Type valueType) {
        return (valueType == typeof(int) || valueType == typeof(float))
               && TOp.PerformOpCode is not null
               && TOp.CanUseOpCodeFor(ConditionA, ConditionB);
    }

    protected void EmitPerform(ConditionCompilationCtx ctx, Type valueType, Type targetType) {
        if (valueType == typeof(object)) {
            ctx.Il.Emit(OpCodes.Call, Method_Dispatch);
            ctx.Il.EmitConvertToInSessionExpression(typeof(object), targetType);
            return;
        }

        if (CanUseOpcode(valueType)) {
            ctx.Il.Emit(TOp.PerformOpCode!.Value);
            ctx.Il.EmitConvertToInSessionExpression(valueType, targetType);
            return;
        }

        if (valueType == typeof(int)) {
            ctx.Il.Emit(OpCodes.Call, Method_TOp_Perform_T_T.MakeGenericMethod(valueType));
            ctx.Il.EmitConvertToInSessionExpression(valueType, targetType);
            return;
        }

        if (valueType == typeof(float)) {
            ctx.Il.Emit(OpCodes.Call, Method_TOp_Perform_T_T.MakeGenericMethod(valueType));
            ctx.Il.EmitConvertToInSessionExpression(valueType, targetType);
            return;
        }

        throw new NotImplementedException($"{valueType}");
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        var innerType = ReturnType ?? typeof(object);

        if (innerType == typeof(Vector2)) {
            var aIsVector = ConditionA.ReturnType == typeof(Vector2);
            var bIsVector = ConditionB.ReturnType == typeof(Vector2);
            EmitGetValuesFromChildConditions(ctx, aIsVector ? ConditionA.ReturnType! : typeof(float),
                bIsVector ? ConditionB.ReturnType : typeof(float));
            ctx.Il.Emit(OpCodes.Call, (aIsVector, bIsVector) switch {
                (true, true) => Method_TOp_Perform_Vector2_Vector2,
                (false, true) => Method_TOp_Perform_float_Vector2,
                (true, false) => Method_TOp_Perform_Vector2_float,
                (false, false) => throw new UnreachableException(),
            });
            ctx.Il.EmitConvertToInSessionExpression(typeof(Vector2), targetType);

            return;
        }

        EmitGetValuesFromChildConditions(ctx, innerType);
        EmitPerform(ctx, innerType, targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => InnerConditionsUseCurrentConditionLocalInEmit;

    private static object LogIncomparableTypes(object a, object b) {
        NotificationHelper.Notify(
            $"Can't perform math on objects of types: {a.GetType()} and {b.GetType()}. Result will always be 0!");
        return 0;
    }

    protected internal override Type? ReturnType { get; } =
        condA.ReturnType is { } tA && condB.ReturnType is { } tB ? GetReturnType(tA, tB) : null;

    private static Type? GetReturnType(Type a, Type b) {
        if (a == b)
            return a;

        if (a == typeof(int) && b == typeof(float))
            return typeof(float);
        if (a == typeof(float) && b == typeof(int))
            return typeof(float);
        if (a == typeof(Vector2) && (b == typeof(int) || b == typeof(float)))
            return typeof(Vector2);
        if (b == typeof(Vector2) && (a == typeof(int) || a == typeof(float)))
            return typeof(Vector2);
        return null;
    }
}

internal struct IOperatorModulo : IMathOperator {
    public static T Perform<T>(T a, T b) where T : INumber<T> {
        if (T.IsZero(b))
            return T.Zero;
        
        return a % b;
    }
        
    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a % b.X, a % b.Y);
    }
        
    public static Vector2 Perform(Vector2 a, float b) {
        return new Vector2(a.X % b, a.Y % b);
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return new Vector2(a.X % b.X, a.Y % b.Y);
    }

    public static OpCode? PerformOpCode => OpCodes.Rem;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return b is IConstCondition<float> { Value: not 0 };
    }
}

internal struct OperatorAdd : IMathOperator {
    public static T Perform<T>(T a, T b) where T : INumber<T> {
        return a + b;
    }

    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a + b.X, a + b.Y);
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return new Vector2(a.X + b, a.Y + b);
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a + b;
    }

    public static OpCode? PerformOpCode => OpCodes.Add;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return true;
    }
}

internal struct OperatorSub : IMathOperator {
    public static T Perform<T>(T a, T b) where T : INumber<T> {
        return a - b;
    }

    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a - b.X, a - b.Y);
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return new Vector2(a.X - b, a.Y - b);
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a - b;
    }

    public static OpCode? PerformOpCode => OpCodes.Sub;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return true;
    }
}

internal struct OperatorMul : IMathOperator {
    public static T Perform<T>(T a, T b) where T : INumber<T> {
        return a * b;
    }

    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a * b.X, a * b.Y);
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return new Vector2(a.X * b, a.Y * b);
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a * b;
    }

    public static OpCode? PerformOpCode => OpCodes.Mul;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return true;
    }
}

internal struct OperatorDiv : IMathOperator {
    public static T Perform<T>(T a, T b) where T : INumber<T> {
        if (T.IsZero(b)) {
            return T.Zero;
        }

        return a / b;
    }

    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a / b.X, a / b.Y);
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return a / b;
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a / b;
    }

    public static OpCode? PerformOpCode => OpCodes.Div;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return b is IConstCondition<float> { Value: not 0 };
    }
}

internal sealed class OperatorDivFloat(ConditionHelper.Condition a, ConditionHelper.Condition b)
    : ConditionHelper.BinaryOperator(a, b) {
    public static T Perform<T>(T a, T b) where T : INumber<T> {
        if (T.IsZero(b)) {
            return T.Zero;
        }

        return a / b;
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return a / b;
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a / b;
    }

    protected internal override Type? ReturnType { get; } =
        a.ReturnType is { } tA && b.ReturnType is { } tB ? GetReturnType(tA, tB) : null;

    private static Type? GetReturnType(Type a, Type b) {
        if (a == b)
            return a;

        if (a == typeof(int) && b == typeof(float))
            return typeof(float);
        if (a == typeof(float) && b == typeof(int))
            return typeof(float);
        if (a == typeof(Vector2) && (b == typeof(int) || b == typeof(float)))
            return typeof(Vector2);
        if (b == typeof(Vector2) && (a == typeof(int) || a == typeof(float)))
            return typeof(Vector2);
        return null;
    }

    private static readonly MethodInfo MethodPerform =
        typeof(OperatorDivFloat).GetMethod(nameof(Perform), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo MethodDivPerform_T = typeof(OperatorDiv)
        .GetMethod(nameof(OperatorDiv.Perform), 1, BindingFlags.Static | BindingFlags.Public, null,
            [Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0)], null)!;


    private static object Perform(object a, object b) {
        return (a, b) switch {
            (int ai, int bi) => OperatorDiv.Perform((float) ai, bi),
            (float ai, float bi) => OperatorDiv.Perform(ai, bi),
            (float bi, Vector2 v2) => OperatorDiv.Perform(bi, v2),
            (int bi, Vector2 v2) => OperatorDiv.Perform(bi, v2),
            (Vector2 v2, int bi) => OperatorDiv.Perform(v2, bi),
            (Vector2 v2, float bi) => OperatorDiv.Perform(v2, bi),
            (Vector2 v2, Vector2 bi) => OperatorDiv.Perform(v2, bi),
            _ => LogIncomparableTypes(a, b)
        };
    }

    protected override object Operate(object a, object b) {
        return Perform(a, b);
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        var aType = ConditionA.ReturnType;
        var bType = ConditionB.ReturnType;
        var aIsNumber = aType == typeof(int) || aType == typeof(float);
        var bIsNumber = bType == typeof(int) || bType == typeof(float);
        if (aIsNumber && bIsNumber) {
            if (ConditionB is IConstCondition<float> { Value: not 0 }) {
                EmitGetValuesFromChildConditions(ctx, typeof(float));
                ctx.Il.Emit(OpCodes.Div);
                ctx.Il.EmitConvertToInSessionExpression(typeof(float), targetType);
                return;
            }

            EmitGetValuesFromChildConditions(ctx, typeof(float));
            ctx.Il.Emit(OpCodes.Call, MethodDivPerform_T.MakeGenericMethod(typeof(float)));
            ctx.Il.EmitConvertToInSessionExpression(typeof(float), targetType);
            return;
        }

        EmitGetValuesFromChildConditions(ctx, typeof(object));
        ctx.Il.Emit(OpCodes.Call, MethodPerform);
        ctx.Il.EmitConvertToInSessionExpression(typeof(object), targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => InnerConditionsUseCurrentConditionLocalInEmit;

    private static object LogIncomparableTypes(object a, object b) {
        NotificationHelper.Notify(
            $"Can't perform math on objects of types: {a.GetType()} and {b.GetType()}. Result will always be 0!");
        return 0;
    }
}
