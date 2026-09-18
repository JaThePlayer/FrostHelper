using FrostHelper.Helpers;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace FrostHelper.SessionExpressions;

internal interface IMathOperator<out TIntIntResult>
    : IMathOperator<float, float, float>,
        IMathOperator<float, int, float>,
        IMathOperator<int, float, float>,
        IMathOperator<int, int, TIntIntResult>,
        ITypeByNumberMathOperatorLeft<Vector2>,
        ITypeByNumberMathOperatorRight<Vector2>,
        IMathOperator<Vector2, Vector2, Vector2>;

// We need to split ITypeByNumberMathOperator to left and right portions to avoid a compiler error from a potential interface method overlaps.
internal interface ITypeByNumberMathOperatorLeft<TType>
    : IMathOperator<float, TType, TType>,
      IMathOperator<int, TType, TType>;

internal interface ITypeByNumberMathOperatorRight<TType>
    : IMathOperator<TType, float, TType>,
        IMathOperator<TType, int, TType>;

internal interface IMathOperator<in TLeft, in TRight, out TRet> {
    static abstract TRet Perform(TLeft a, TRight b);
    
    static abstract OpCode? PerformOpCode { get; }

    static abstract bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b);
}

internal static class MathOperatorRegistry {
    static MathOperatorRegistry() {
        Registry = new();

        RegisterDefaultMathOperator<int, OperatorAdd>(BinOpExpression.Operators.Add);
        RegisterDefaultMathOperator<int, OperatorSub>(BinOpExpression.Operators.Sub);
        RegisterDefaultMathOperator<int, OperatorDiv>(BinOpExpression.Operators.Div);
        RegisterDefaultMathOperator<float, OperatorDivFloat>(BinOpExpression.Operators.DivFloat);
        RegisterDefaultMathOperator<int, OperatorMul>(BinOpExpression.Operators.Mul);
        RegisterDefaultMathOperator<int, IOperatorModulo>(BinOpExpression.Operators.Modulo);

        RegisterTypeByNumber<Color, OperatorMulColor>(BinOpExpression.Operators.Mul);
    }

    static void Register<TLeft, TRight, TRes, TOperator>(BinOpExpression.Operators op)
        where TOperator : IMathOperator<TLeft, TRight, TRes> {
        Registry.TryAdd(op, new());

        var reg = Registry[op];
        reg[(typeof(TLeft), typeof(TRight))] = MathOperator<TLeft, TRight, TRes, TOperator>.Create;
    }

    static void RegisterTypeByNumber<TType, TOperator>(BinOpExpression.Operators op)
        where TOperator : ITypeByNumberMathOperatorLeft<TType>, ITypeByNumberMathOperatorRight<TType> {
        Registry.TryAdd(op, new());
        
        var reg = Registry[op];
        reg[(typeof(float), typeof(TType))] = MathOperator<float, TType, TType, TOperator>.Create;
        reg[(typeof(int), typeof(TType))] = MathOperator<int, TType, TType, TOperator>.Create;
        reg[(typeof(TType), typeof(float))] = MathOperator<TType, float, TType, TOperator>.Create;
        reg[(typeof(TType), typeof(int))] = MathOperator<TType, int, TType, TOperator>.Create;
    }
    
    static void RegisterDefaultMathOperator<TIntIntResult, TOperator>(BinOpExpression.Operators op) where TOperator : IMathOperator<TIntIntResult> {
        Registry.TryAdd(op, new());

        var reg = Registry[op];
        reg[(typeof(int), typeof(int))] = MathOperator<int, int, TIntIntResult, TOperator>.Create;
        reg[(typeof(int), typeof(float))] = MathOperator<int, float, float, TOperator>.Create;
        reg[(typeof(float), typeof(int))] = MathOperator<float, int, float, TOperator>.Create;
        reg[(typeof(float), typeof(float))] = MathOperator<float, float, float, TOperator>.Create;
        reg[(typeof(Vector2), typeof(float))] = MathOperator<Vector2, float, Vector2, TOperator>.Create;
        reg[(typeof(float), typeof(Vector2))] = MathOperator<float, Vector2, Vector2, TOperator>.Create;
        reg[(typeof(Vector2), typeof(int))] = MathOperator<Vector2, int, Vector2, TOperator>.Create;
        reg[(typeof(int), typeof(Vector2))] = MathOperator<int, Vector2, Vector2, TOperator>.Create;
        reg[(typeof(Vector2), typeof(Vector2))] = MathOperator<Vector2, Vector2, Vector2, TOperator>.Create;

        reg[(typeof(object), typeof(object))] = (a, b) => new DynamicMathOperator(op, a, b);
    }
    
    internal static Dictionary<BinOpExpression.Operators, 
        Dictionary<(Type left, Type right), Func<ConditionHelper.Condition, ConditionHelper.Condition, ConditionHelper.BinaryOperator>>> Registry { get; }

    public static ConditionHelper.BinaryOperator CreateFor(BinOpExpression.Operators op, ConditionHelper.Condition condA, ConditionHelper.Condition condB) {
        var leftT = condA.ReturnType ?? typeof(object);
        var rightT = condB.ReturnType ?? typeof(object);

        return CreateFor(op, condA, condB, leftT, rightT);
    }
    
    public static ConditionHelper.BinaryOperator CreateFor(BinOpExpression.Operators op, ConditionHelper.Condition condA, ConditionHelper.Condition condB, Type leftT, Type rightT) {
        var available = Registry[op];

        if (leftT == typeof(object) || rightT == typeof(object)) {
            leftT = typeof(object);
            rightT = typeof(object);
        }

        if (available.TryGetValue((leftT, rightT), out var factory)) {
            return factory(condA, condB);
        }

        NotificationHelper.Notify($"Cannot perform {op} between {TypeDescriptor.For(leftT)} and {TypeDescriptor.For(rightT)}.");
        return EmptyBinaryOp.Instance;
    }
}

internal sealed class EmptyBinaryOp() : ConditionHelper.BinaryOperator(ConditionHelper.EmptyCondition, ConditionHelper.EmptyCondition) {
    public static EmptyBinaryOp Instance { get; } = new();

    protected override bool CoerceMismatchedIntFloat => false;

    public override object Operate(object a, object b) {
        return Zero;
    }
}

internal sealed class DynamicMathOperator(BinOpExpression.Operators op, ConditionHelper.Condition condA, ConditionHelper.Condition condB) 
    : ConditionHelper.BinaryOperator(condA, condB) {
    protected override bool CoerceMismatchedIntFloat => false;

    public override object Operate(object a, object b) {
        var cond = MathOperatorRegistry.CreateFor(op, ConditionA, ConditionB, a.GetType(), b.GetType());

        return cond.Operate(a, b);
    }
}

internal sealed class MathOperator<TLeft, TRight, TRet, TOp>(ConditionHelper.Condition condA, ConditionHelper.Condition condB) 
    : ConditionHelper.BinaryOperator(condA, condB) where TOp : IMathOperator<TLeft, TRight, TRet> {
    internal static ConditionHelper.BinaryOperator Create(ConditionHelper.Condition left, ConditionHelper.Condition right) {
        return new MathOperator<TLeft, TRight, TRet, TOp>(left, right);
    }
    
    private static readonly MethodInfo MethodTOpPerform = typeof(TOp)
            .GetMethod(nameof(TOp.Perform), BindingFlags.Static | BindingFlags.Public, [ typeof(TLeft), typeof(TRight) ])!;

    protected override bool CoerceMismatchedIntFloat => false;

    public override object Operate(object a, object b) {
        return TOp.Perform((TLeft) a, (TRight) b)!; 
    }

    internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
        if (TOp.PerformOpCode is { } opCode && TOp.CanUseOpCodeFor(ConditionA, ConditionB)) {
            if ((typeof(TLeft) == typeof(int) && typeof(TRight) == typeof(float))
                || (typeof(TLeft) == typeof(float) && typeof(TRight) == typeof(int))
                || (typeof(TOp) == typeof(OperatorDivFloat) && typeof(TLeft) == typeof(int) && typeof(TRight) == typeof(int))) {
                EmitGetValuesFromChildConditions(ctx, typeof(float), typeof(float));
            } else {
                EmitGetValuesFromChildConditions(ctx, typeof(TLeft), typeof(TRight));
            }
            
            ctx.Il.Emit(opCode);
            ctx.Il.EmitConvertToInSessionExpression(ReturnType, targetType);
            return;
        }

        EmitGetValuesFromChildConditions(ctx, typeof(TLeft), typeof(TRight));
        ctx.Il.Emit(OpCodes.Call, MethodTOpPerform);
        ctx.Il.EmitConvertToInSessionExpression(ReturnType, targetType);
    }

    internal override bool UsesCurrentConditionLocalInEmit => InnerConditionsUseCurrentConditionLocalInEmit;

    protected internal override Type ReturnType { get; } = typeof(TRet);
}

internal struct IOperatorModulo : IMathOperator<int> {
    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a % b.X, a % b.Y);
    }
        
    public static Vector2 Perform(Vector2 a, float b) {
        return new Vector2(a.X % b, a.Y % b);
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return new Vector2(a.X % b.X, a.Y % b.Y);
    }

    public static float Perform(float a, float b) {
        if (b == 0f)
            return 0f;
        
        return a % b;
    }

    public static float Perform(float a, int b) {
        if (b == 0)
            return 0f;
        
        return a % b;
    }

    public static float Perform(int a, float b) {
        if (b == 0f)
            return 0f;
        
        return a % b;
    }

    public static int Perform(int a, int b) {
        if (b == 0)
            return 0;
        
        return a % b;
    }

    public static Vector2 Perform(int a, Vector2 b) {
        return new Vector2(a % b.X, a % b.Y);
    }

    public static Vector2 Perform(Vector2 a, int b) {
        return new Vector2(a.X % b, a.Y % b);
    }

    public static OpCode? PerformOpCode => OpCodes.Rem;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return a.ReturnTypeIsNumber && b is IConstCondition<float> { Value: not 0 };
    }
}

internal struct OperatorAdd : IMathOperator<int> {
    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a + b.X, a + b.Y);
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return new Vector2(a.X + b, a.Y + b);
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a + b;
    }

    public static float Perform(float a, float b) {
        return a + b;
    }

    public static float Perform(float a, int b) {
        return a + b;
    }

    public static float Perform(int a, float b) {
        return a + b;
    }

    public static int Perform(int a, int b) {
        return a + b;
    }

    public static Vector2 Perform(int a, Vector2 b) {
        return new Vector2(a + b.X, a + b.Y);
    }

    public static Vector2 Perform(Vector2 a, int b) {
        return new Vector2(a.X + b, a.Y + b);
    }

    public static OpCode? PerformOpCode => OpCodes.Add;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return a.ReturnTypeIsNumber && b.ReturnTypeIsNumber;
    }
}

internal struct OperatorSub : IMathOperator<int> {
    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a - b.X, a - b.Y);
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return new Vector2(a.X - b, a.Y - b);
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a - b;
    }

    public static float Perform(float a, float b) {
        return a - b;
    }

    public static float Perform(float a, int b) {
        return a - b;
    }

    public static float Perform(int a, float b) {
        return a - b;
    }

    public static int Perform(int a, int b) {
        return a - b;
    }

    public static Vector2 Perform(int a, Vector2 b) {
        return new Vector2(a - b.X, a - b.Y);
    }

    public static Vector2 Perform(Vector2 a, int b) {
        return new Vector2(a.X - b, a.Y - b);
    }

    public static OpCode? PerformOpCode => OpCodes.Sub;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return a.ReturnTypeIsNumber && b.ReturnTypeIsNumber;
    }
}

internal struct OperatorMul : IMathOperator<int> {
    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a * b.X, a * b.Y);
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return new Vector2(a.X * b, a.Y * b);
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a * b;
    }

    public static float Perform(float a, float b) {
        return a * b;
    }

    public static float Perform(float a, int b) {
        return a * b;
    }

    public static float Perform(int a, float b) {
        return a * b;
    }

    public static int Perform(int a, int b) {
        return a * b;
    }

    public static Vector2 Perform(int a, Vector2 b) {
        return new Vector2(a * b.X, a * b.Y);
    }

    public static Vector2 Perform(Vector2 a, int b) {
        return new Vector2(a.X * b, a.Y * b);
    }

    public static OpCode? PerformOpCode => OpCodes.Mul;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return a.ReturnTypeIsNumber && b.ReturnTypeIsNumber;
    }
}

internal struct OperatorDiv : IMathOperator<int> {
    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a / b.X, a / b.Y);
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return a / b;
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a / b;
    }

    public static float Perform(float a, float b) {
        if (b == 0)
            return 0;
        return a / b;
    }

    public static float Perform(float a, int b) {
        if (b == 0)
            return 0;
        return a / b;
    }

    public static float Perform(int a, float b) {
        if (b == 0)
            return 0;
        return a / b;
    }

    public static int Perform(int a, int b) {
        if (b == 0)
            return 0;
        return a / b;
    }

    public static Vector2 Perform(int a, Vector2 b) {
        return new Vector2(a / b.X, a / b.Y);
    }

    public static Vector2 Perform(Vector2 a, int b) {
        return a / b;
    }

    public static OpCode? PerformOpCode => OpCodes.Div;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return a.ReturnTypeIsNumber && b is IConstCondition<float> { Value: not 0 };
    }
}

internal struct OperatorDivFloat : IMathOperator<float> {
    public static Vector2 Perform(float a, Vector2 b) {
        return new Vector2(a / b.X, a / b.Y);
    }

    public static Vector2 Perform(Vector2 a, float b) {
        return a / b;
    }

    public static Vector2 Perform(Vector2 a, Vector2 b) {
        return a / b;
    }

    public static float Perform(float a, float b) {
        if (b == 0)
            return 0;
        return a / b;
    }

    public static float Perform(float a, int b) {
        if (b == 0)
            return 0;
        return a / b;
    }

    public static float Perform(int a, float b) {
        if (b == 0)
            return 0;
        return a / b;
    }

    public static float Perform(int a, int b) {
        if (b == 0)
            return 0;
        return a / (float)b;
    }

    public static Vector2 Perform(int a, Vector2 b) {
        return new Vector2(a / b.X, a / b.Y);
    }

    public static Vector2 Perform(Vector2 a, int b) {
        return a / b;
    }

    public static OpCode? PerformOpCode => OpCodes.Div;

    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return a.ReturnTypeIsNumber && b is IConstCondition<float> { Value: not 0 };
    }
}

internal struct OperatorMulColor : ITypeByNumberMathOperatorLeft<Color>, ITypeByNumberMathOperatorRight<Color> {
    public static Color Perform(Color a, float b) {
        return a * b;
    }

    public static Color Perform(float a, Color b) {
        return b * a;
    }

    public static Color Perform(int a, Color b) {
        return b * a;
    }

    public static Color Perform(Color a, int b) {
        return a * b;
    }

    public static OpCode? PerformOpCode => null;
    
    public static bool CanUseOpCodeFor(ConditionHelper.Condition a, ConditionHelper.Condition b) {
        return false;
    }
}
