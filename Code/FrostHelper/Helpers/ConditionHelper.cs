using FrostHelper.ModIntegration;
using FrostHelper.SessionExpressions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Vector2 = Microsoft.Xna.Framework.Vector2;

using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.Helpers;

public static class ConditionHelper {
    internal static readonly Condition EmptyCondition = new Empty();

    internal static readonly Condition TrueCondition = new ConstInt(1);
    internal static readonly Condition FalseCondition = new ConstInt(0);

    internal static Condition CreateOrDefault(string txt, string defaultValue, ExpressionContext? ctx = null) {
        ctx ??= ExpressionContext.Default;
        
        if (TryCreate(txt, ctx, out var cond))
            return cond;
        if (TryCreate(defaultValue, ctx, out cond))
            return cond;
        
        NotificationHelper.Notify($"Default condition is malformed, this is a Frost Helper bug!\n{defaultValue}\n{new StackTrace()}");
        return EmptyCondition;
    }
    
    internal static bool TryCreate(string str, ExpressionContext ctx, [NotNullWhen(true)] out Condition? condition) {
        if (string.IsNullOrWhiteSpace(str)) {
            condition = EmptyCondition;
            return true;
        }

        if (AbstractExpression.TryParseCached(str, out var expr)) {
            return TryCreate(expr, ctx, out condition);
        }

        condition = null;
        return false;
    }

    private static bool CreateList(IList<AbstractExpression> args, ExpressionContext ctx, out List<Condition> conditions) {
        conditions = new List<Condition>(args.Count);
        foreach (var argExpr in args) {
            if (!TryCreate(argExpr, ctx, out var argCond)) {
                return false;
            }
            conditions.Add(argCond);
        }

        return true;
    }
    
    private static bool TryCreate(AbstractExpression expr, ExpressionContext ctx, [NotNullWhen(true)] out Condition? condition) {
        
        switch (expr)
        {
            case SimpleCommandExpression simpleCmd when simpleCmd.Name.StartsWith("input."):
                return InputCommands.TryParseInput(simpleCmd.Name["input.".Length..], out condition);
            case SimpleCommandExpression simpleCmd: {
                var remaining = simpleCmd.Name;
                List<string>? fields = null;
                condition = null;
                while (true) {
                    // Try simple commands from the context
                    if (ctx.SimpleCommands.TryGetValue(remaining, out var cond)) {
                        condition = cond;
                        break;
                    }
                    
                    // Try simple commands
                    if (SimpleCommands.Registry.TryGetValue(remaining, out cond)) {
                        condition = cond;
                        break;
                    }
                    
                    var lastDotIdx = remaining.LastIndexOf('.');
                    if (lastDotIdx == -1)
                        break;
                    fields ??= [];
                    fields.Add(remaining[(lastDotIdx+1)..]);
                    remaining = remaining[..lastDotIdx];
                }

                if (condition is null) {
                    NotificationHelper.Notify($"Unknown use of the $ operator: {expr}");
                    return false;
                }

                while (fields?.Count > 0) {
                    condition = FieldAccessCommands.Create(fields[^1], condition, ctx);
                    fields.RemoveAt(fields.Count - 1);
                }

                return true;
            }
            case GetSessionVariableExpression sessVarExpr:
            {
                if (!TryCreate(sessVarExpr.Name, ctx, out var nameCond)) {
                    condition = null;
                    return false;
                }
            
                switch (sessVarExpr.VariableType) {
                    case GetSessionVariableExpression.Types.Flag:
                        condition = new FlagAccessor(nameCond, inverted: false);
                        return true;
                    case GetSessionVariableExpression.Types.Counter:
                        condition = nameCond is ConstString { Value: var n } ? new CounterAccessor(n) : new IndirectCounterAccessor(nameCond);
                        return true;
                    case GetSessionVariableExpression.Types.Slider:
                        condition = nameCond is ConstString { Value: var sn } ? new SliderAccessor(sn) : new IndirectSliderAccessor(nameCond);
                        return true;
                }

                break;
            }
            case InvertExpression invertExpression:
            {
                if (!TryCreate(invertExpression.Expression, ctx, out var invertCond)) {
                    condition = null;
                    return false;
                }
            
                if (invertCond is IInvertible invertible)
                    condition = invertible.CreateInverted();
                else
                    condition = new OperatorInvert(invertCond);
                return true;
            }
            case FunctionCommandExpression { Name: { } funcName, Arguments: { } args }:
            {
                if (!CreateList(args, ctx, out var argConds)) {
                    condition = null;
                    return false;
                }
            
                return FunctionCommands.TryCreate(funcName, argConds, ctx, out condition);
            }
            case InterpolatedStringExpression { Arguments: { } strArgs }:
            {
                if (!CreateList(strArgs, ctx, out var argConds)) {
                    condition = null;
                    return false;
                }

                condition = new StringInterpolationOperator(argConds);
                return true;
            }
            case LiteralExpression<string> stringLit:
                condition = new ConstString(stringLit.Value);
                return true;
            case LiteralExpression<int> intLit:
                condition = new ConstInt(intLit.Value);
                return true;
            case LiteralExpression<float> floatLit:
                condition = new ConstFloat(floatLit.Value);
                return true;
            case BinOpExpression { Left: { } left, Right: { } right } binExpr:
            {
                if (!TryCreate(left, ctx, out var leftExpr)) {
                    condition = null;
                    return false;
                }
                if (!TryCreate(right, ctx, out var rightExpr)) {
                    condition = null;
                    return false;
                }

                condition = binExpr.Operator switch {
                    BinOpExpression.Operators.And => new OperatorAnd(leftExpr, rightExpr),
                    BinOpExpression.Operators.Or => new OperatorOr(leftExpr, rightExpr),
                    BinOpExpression.Operators.BitwiseAnd => new BitwiseOperator<OperatorBitwiseAnd>(leftExpr, rightExpr),
                    BinOpExpression.Operators.BitwiseOr => new BitwiseOperator<OperatorBitwiseOr>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Add => new MathOperator<OperatorAdd>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Sub => new MathOperator<OperatorSub>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Mul => new MathOperator<OperatorMul>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Div => new MathOperator<OperatorDiv>(leftExpr, rightExpr),
                    BinOpExpression.Operators.DivFloat => new OperatorDivFloat(leftExpr, rightExpr),
                    BinOpExpression.Operators.Modulo => new MathOperator<IOperatorModulo>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Lt => new ComparisonOperator<OperatorLt>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Gt => new ComparisonOperator<OperatorGt>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Eq => new ComparisonOperator<OperatorEq>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Ne => new ComparisonOperator<OperatorNe>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Ge => new ComparisonOperator<OperatorGte>(leftExpr, rightExpr),
                    BinOpExpression.Operators.Le => new ComparisonOperator<OperatorLte>(leftExpr, rightExpr),
                    _ => null
                };

                if (condition is null) {
                    NotificationHelper.Notify($"Unknown operator: {binExpr.Operator}");
                    return false;
                }

                return true;
            }
            case FieldAccessExpression { Name: var fieldName, ObjectExpression: var objectExpression }: {
                if (!TryCreate(objectExpression, ctx, out var objExpr)) {
                    condition = null;
                    return false;
                }

                condition = FieldAccessCommands.Create(fieldName, objExpr, ctx);
                return true;
            }
        }

        NotificationHelper.Notify($"Couldn't parse: {expr}");
        condition = null;
        return false;
    }

    internal sealed class StringInterpolationOperator(List<Condition> args) : Condition {
        private Condition GetArg(int index) => args[index];
        
        private static readonly MethodInfo MethodGetArg = typeof(StringInterpolationOperator).GetMethod(nameof(GetArg), BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        private static readonly MethodInfo MethodInterpolatorHandlerAppendLiteralString
            = typeof(Interpolator.Handler).GetMethod(nameof(Interpolator.Handler.AppendLiteral), BindingFlags.Instance | BindingFlags.Public, [typeof(string)])!;
        
        private static readonly MethodInfo MethodInterpolatorHandlerAppendFormattedObject
            = typeof(Interpolator.Handler).GetMethod(nameof(Interpolator.Handler.AppendFormatted), BindingFlags.Instance | BindingFlags.Public, [typeof(object)])!;
        
        private static readonly MethodInfo MethodInterpolatorHandlerAppendFormattedT_ISpanFormattable
            = typeof(Interpolator.Handler).GetMethod(nameof(Interpolator.Handler.AppendFormatted), 1, BindingFlags.Instance | BindingFlags.Public, null, [Type.MakeGenericMethodParameter(0)], null)!;

        private static readonly MethodInfo MethodInterpolatorHandlerResultToString
            = typeof(Interpolator.Handler).GetMethod(nameof(Interpolator.Handler.ResultToString), BindingFlags.Instance | BindingFlags.Public)!;

        
        public override object Get(Session session, object? userdata) {
            Interpolator.Handler handler = new Interpolator.Handler(0, args.Count, Interpolator.Shared);
            foreach (var arg in args) {
                var obj = arg.Get(session, userdata);
                if (obj is string str)
                    handler.AppendLiteral(str);
                else
                    handler.AppendFormatted(obj);
            }
            
            return handler.ResultToString();
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            var handlerLocal = il.DeclareLocal(typeof(Interpolator.Handler));
            LocalBuilder? tempLocal = null;
            il.Emit(OpCodes.Ldloca, handlerLocal);
            il.Emit(OpCodes.Ldc_I4_0); // literal length
            il.Emit(OpCodes.Ldc_I4, args.Count); // formatted length
            il.Emit(OpCodes.Call, typeof(Interpolator).GetProperty(nameof(Interpolator.Shared))!.GetMethod!);
            il.Emit(OpCodes.Call, typeof(Interpolator.Handler).GetConstructor([typeof(int), typeof(int), typeof(Interpolator)])!);

            var argI = 0;
            foreach (var arg in args) {
                il.EmitSwapOutCurrentCondition(ref tempLocal, ctx, arg, () => {
                    il.Emit(OpCodes.Ldc_I4, argI);
                    il.Emit(OpCodes.Call, MethodGetArg);
                });
                
                il.Emit(OpCodes.Ldloca, handlerLocal);
                
                var argType = arg.ReturnType ?? typeof(object);
                arg.Emit(ctx, argType);
                if (argType == typeof(string)) {
                    il.Emit(OpCodes.Call, MethodInterpolatorHandlerAppendLiteralString);
                }
                else if (argType.IsAssignableTo(typeof(ISpanFormattable))) {
                    il.Emit(OpCodes.Call, MethodInterpolatorHandlerAppendFormattedT_ISpanFormattable.MakeGenericMethod(argType));
                }
                else {
                    if (argType.IsValueType)
                        il.Emit(OpCodes.Box, argType);
                    il.Emit(OpCodes.Call, MethodInterpolatorHandlerAppendFormattedObject);
                }

                argI++;
            }
            
            il.EmitRevertCurrentCondition(tempLocal, ctx);
            
            il.Emit(OpCodes.Ldloca, handlerLocal);
            il.Emit(OpCodes.Call, MethodInterpolatorHandlerResultToString);
            il.EmitConvertToInSessionExpression(typeof(string), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit { get; }
            = args.Any(a => a.UsesCurrentConditionLocalInEmit);

        protected internal override Type ReturnType => typeof(string);

        protected override IEnumerable<object> GetArgsForDebugPrint() => args;
    }

    internal sealed class OperatorAnd(Condition a, Condition b) : Condition {
        private readonly Condition _a = a, _b = b;
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
    
    internal sealed class OperatorOr(Condition a, Condition b) : Condition {
        private readonly Condition _a = a, _b = b;
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

    internal sealed class BitwiseOperator<TOp>(Condition condA, Condition condB) : BinaryOperator(condA, condB) where TOp : IBitwiseOperator {
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
        
        public static bool CanUseOpCodeFor(Condition a, Condition b) {
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
        
        public static bool CanUseOpCodeFor(Condition a, Condition b) {
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
        
        public static bool CanUseOpCodeFor(Condition a, Condition b) {
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
        
        public static bool CanUseOpCodeFor(Condition a, Condition b) {
            return b is IConstCondition<float> { Value: not 0 };
        }
    }
    
    internal sealed class OperatorDivFloat(Condition a, Condition b) : BinaryOperator(a, b) {
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

        private static readonly MethodInfo MethodPerform = typeof(OperatorDivFloat).GetMethod(nameof(Perform), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo MethodDivPerform_T = typeof(OperatorDiv)
            .GetMethod(nameof(OperatorDiv.Perform), 1, BindingFlags.Static | BindingFlags.Public, null, [ Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) ], null)!;

        
        private static object Perform(object a, object b) {
            return (a, b) switch {
                (int ai, int bi) => OperatorDiv.Perform((float)ai, bi),
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
    
    internal struct IOperatorModulo : IMathOperator {
        public static T Perform<T>(T a, T b) where T : INumber<T> {
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
        
        public static bool CanUseOpCodeFor(Condition a, Condition b) {
            return true;
        }
    }

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

        public static List<OpCode> OpCodeSequence { get; } = [ OpCodes.Ceq ];
    }
    
    internal struct OperatorNe : IComparisonOperator {
        public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
            return a != b;
        }

        public static bool Compare(string a, string b) {
            return a != b;
        }
        
        public static List<OpCode> OpCodeSequence { get; } = [ OpCodes.Ceq, OpCodes.Ldc_I4_0, OpCodes.Ceq ];
    }
    
    internal struct OperatorGt : IComparisonOperator {
        public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
            return a > b;
        }

        public static bool Compare(string a, string b) {
            return a.CompareTo(b, StringComparison.InvariantCulture) > 0;
        }
        
        public static List<OpCode> OpCodeSequence { get; } = [ OpCodes.Cgt ];
    }
    
    internal struct OperatorLt : IComparisonOperator {
        public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
            return a < b;
        }
        
        public static bool Compare(string a, string b) {
            return a.CompareTo(b, StringComparison.InvariantCulture) < 0;
        }
        
        public static List<OpCode> OpCodeSequence { get; } = [ OpCodes.Clt ];
    }
    
    internal struct OperatorGte : IComparisonOperator {
        public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
            return a >= b;
        }
        
        public static bool Compare(string a, string b) {
            return a.CompareTo(b, StringComparison.InvariantCulture) >= 0;
        }
        
        public static List<OpCode> OpCodeSequence { get; } = [ OpCodes.Clt, OpCodes.Ldc_I4_0, OpCodes.Ceq ];
    }
    
    internal struct OperatorLte : IComparisonOperator {
        public static bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool> {
            return a <= b;
        }
        
        public static bool Compare(string a, string b) {
            return a.CompareTo(b, StringComparison.InvariantCulture) <= 0;
        }
        
        public static List<OpCode> OpCodeSequence { get; } =  [ OpCodes.Cgt, OpCodes.Ldc_I4_0, OpCodes.Ceq ];
    }

    internal sealed class ComparisonOperator<TOp>(Condition condA, Condition condB) : BinaryOperator(condA, condB) where TOp : IComparisonOperator {
        private static readonly MethodInfo Method_TOp_Compare_T_T = typeof(TOp)
            .GetMethod(nameof(TOp.Compare), 1, BindingFlags.Static | BindingFlags.Public, null, [ Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) ], null)!;

        private static readonly MethodInfo Method_TOp_Compare_String_String = typeof(TOp)
            .GetMethod(nameof(TOp.Compare), 0, BindingFlags.Static | BindingFlags.Public, null, [ typeof(string), typeof(string) ], null)!;

        private static readonly MethodInfo Method_Dispatch = typeof(ComparisonOperator<TOp>)
            .GetMethod(nameof(Dispatch), 0, BindingFlags.Static | BindingFlags.NonPublic, null, [ typeof(object), typeof(object) ], null)!;

        
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
            NotificationHelper.Notify($"Can't compare objects of types: {a.GetType()} and {b.GetType()}. Result will always be 0!");
            return false;
        }

        protected internal override Type ReturnType => typeof(int);
    }
    
    internal interface IMathOperator {
        static abstract T Perform<T>(T a, T b) where T : INumber<T>;
        
        static abstract Vector2 Perform(float a, Vector2 b);
        
        static abstract Vector2 Perform(Vector2 a, float b);
        
        static abstract Vector2 Perform(Vector2 a, Vector2 b);
        
        static abstract OpCode? PerformOpCode { get; }

        static abstract bool CanUseOpCodeFor(Condition a, Condition b);
    }

    internal sealed class MathOperator<TOp>(Condition condA, Condition condB) : BinaryOperator(condA, condB) where TOp : IMathOperator {
        private static readonly MethodInfo Method_TOp_Perform_T_T = typeof(TOp)
            .GetMethod(nameof(TOp.Perform), 1, BindingFlags.Static | BindingFlags.Public, null, [ Type.MakeGenericMethodParameter(0), Type.MakeGenericMethodParameter(0) ], null)!;

        private static readonly MethodInfo Method_TOp_Perform_float_Vector2 = typeof(TOp)
            .GetMethod(nameof(TOp.Perform), 0, BindingFlags.Static | BindingFlags.Public, null, [ typeof(float), typeof(Vector2) ], null)!;

        private static readonly MethodInfo Method_TOp_Perform_Vector2_float = typeof(TOp)
            .GetMethod(nameof(TOp.Perform), 0, BindingFlags.Static | BindingFlags.Public, null, [ typeof(Vector2), typeof(float) ], null)!;
        
        private static readonly MethodInfo Method_TOp_Perform_Vector2_Vector2 = typeof(TOp)
            .GetMethod(nameof(TOp.Perform), 0, BindingFlags.Static | BindingFlags.Public, null, [ typeof(Vector2), typeof(Vector2) ], null)!;

        
        private static readonly MethodInfo Method_Dispatch = typeof(MathOperator<TOp>)
            .GetMethod(nameof(Dispatch), BindingFlags.Static | BindingFlags.NonPublic, [typeof(object), typeof(object)])!;
        
        protected static object Dispatch(object a, object b) {
            return (a, b) switch {
                (int ai, int bi) => TOp.Perform(ai, bi),
                (float ai, float bi) => TOp.Perform(ai, bi),
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
                EmitGetValuesFromChildConditions(ctx, aIsVector ? ConditionA.ReturnType! : typeof(float), bIsVector ? ConditionB.ReturnType : typeof(float));
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
    
    internal abstract class BinaryOperator(Condition condA, Condition condB) : Condition {
        protected readonly Condition ConditionA = condA;
        protected readonly Condition ConditionB = condB;
        
        
        public override object Get(Session session, object? userdata) {
            var a = ConditionA.Get(session, userdata);
            var b = ConditionB.Get(session, userdata);

            if (a is bool ab)
                a = ab ? 1 : 0;
            if (b is bool bb)
                a = bb ? 1 : 0;

            return (a, b) switch {
                // When floats and ints are mismatched, perform operation on floats
                (int ai, float bi) => Operate((float)ai, (float)bi),
                (float ai, int bi) => Operate((float)ai, (float)bi),
                _ => Operate(a, b)
            };
        }

        protected void EmitGetValuesFromChildConditions(ConditionCompilationCtx ctx, Type targetType, Type? targetTypeB = null) {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;
            targetTypeB ??= targetType;
            
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, ConditionA, typeof(BinaryOperator).GetField(nameof(ConditionA), BindingFlags.Instance | BindingFlags.NonPublic)!);
            ConditionA.Emit(ctx, targetType);
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, ConditionB, typeof(BinaryOperator).GetField(nameof(ConditionB), BindingFlags.Instance | BindingFlags.NonPublic)!);
            ConditionB.Emit(ctx, targetTypeB);
            
            il.EmitRevertCurrentCondition(tempOrigCond, ctx);
        }
        
        protected bool InnerConditionsUseCurrentConditionLocalInEmit => ConditionA.UsesCurrentConditionLocalInEmit ||
                                                                        ConditionB.UsesCurrentConditionLocalInEmit;

        public override bool OnlyChecksFlags() => ConditionA.OnlyChecksFlags() && ConditionB.OnlyChecksFlags();
        
        protected abstract object Operate(object a, object b);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [ConditionA, ConditionB];
    }

    internal sealed class OperatorInvert(Condition x) : Condition {
        public override object Get(Session session, object? userdata) {
            return CoerceToBool(x.Get(session, userdata)) ? 0 : 1;
        }
        
        public override bool OnlyChecksFlags() => x.OnlyChecksFlags();
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [x];
    }

    internal interface IConstCondition;

    internal interface IConstCondition<T> : IConstCondition {
        T Value { get; }
    }

    internal sealed class ConstInt(int x) : Condition, IConstCondition<int>, IConstCondition<float> {
        private readonly object _boxed = x;
        
        public override object Get(Session session, object? userdata) => _boxed;

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            ctx.Il.EmitLoadConstAs(_boxed, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        public override bool OnlyChecksFlags() => true;
        
        public int Value => x;
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [ _boxed ];
        
        float IConstCondition<float>.Value => Value;
    }
    
    internal sealed class ConstFloat(float x) : Condition, IConstCondition<int>, IConstCondition<float> {
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

        int IConstCondition<int>.Value => (int)x;
    }
    
    internal sealed class ConstString(string x) : Condition, IConstCondition<string> {
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

    internal sealed class FlagAccessor(Condition nameCond, bool inverted) : Condition, IInvertible {
        public string? Flag => _nameCondition is ConstString c ? c.Value : null;

        private readonly Condition _nameCondition = nameCond;
        private static readonly FieldInfo FieldNameCondition = typeof(FlagAccessor).GetField(nameof(_nameCondition), BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        public bool Inverted => inverted;
        
        public override bool OnlyChecksFlags() => _nameCondition.OnlyChecksFlags();
        
        private static readonly MethodInfo MethodSessionGetFlag = typeof(Session).GetMethod(nameof(Session.GetFlag))!;
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;

            ctx.EmitLoadSession();
            if (_nameCondition is IConstCondition<string> constStr) {
                il.Emit(OpCodes.Ldstr, constStr.Value);
            } else {
                LocalBuilder? temp = null;
                ctx.Il.EmitSwapOutCurrentCondition(ref temp, ctx, _nameCondition, FieldNameCondition);
                
                _nameCondition.Emit(ctx, typeof(string));
                ctx.Il.EmitRevertCurrentCondition(temp, ctx);
            }
            
            il.Emit(OpCodes.Callvirt, MethodSessionGetFlag);
            if (Inverted) {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
            }
            il.EmitConvertToInSessionExpression(typeof(bool), targetType);
        }
        
        internal override bool UsesCurrentConditionLocalInEmit => _nameCondition.UsesCurrentConditionLocalInEmit;
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [Inverted ? $"!{Flag ?? _nameCondition.ToString()}" : Flag ?? _nameCondition.ToString()];
        
        public Condition CreateInverted() {
            return new FlagAccessor(_nameCondition, !Inverted);
        }
        
        public override object Get(Session session, object? userdata) {
            var flag = Flag ?? _nameCondition.GetString(session, userdata);
            return session.GetFlag(flag) != inverted ? One : Zero;
        }
    }
    
    private sealed class SliderAccessor(string name) : Condition {
        private static readonly MethodInfo MethodSessionGetSlider = typeof(Session).GetMethod(nameof(Session.GetSlider), BindingFlags.Instance | BindingFlags.Public)!;
        
        private WeakReference<Session.Slider>? _slider;
        private WeakReference<Session>? _lastSession;
        
        public override object Get(Session session, object? userdata) {
            if ((_lastSession?.TryGetTarget(out var last) ?? false) && last != session) {
                _slider = null;
                _lastSession = null;
            }
            
            _lastSession ??= new WeakReference<Session>(session);

            if (_slider?.TryGetTarget(out var slider) is not true) {
                slider = session.GetSliderObject(name);
                _slider = new(slider); 
            }

            return slider.Value;
        }
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            ctx.EmitLoadSession();
            ctx.Il.Emit(OpCodes.Ldstr, name);
            ctx.Il.Emit(OpCodes.Callvirt, MethodSessionGetSlider);
            ctx.EmitConvertTo(typeof(float), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        protected internal override Type ReturnType => typeof(float);
    }
    
    private sealed class IndirectSliderAccessor(Condition nameCond) : Condition {
        private readonly Condition _nameCondition = nameCond;
        private static readonly FieldInfo FieldNameCondition = typeof(IndirectSliderAccessor).GetField(nameof(_nameCondition), BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo MethodSessionGetSlider = typeof(Session).GetMethod(nameof(Session.GetSlider), BindingFlags.Instance | BindingFlags.Public)!;

        public override object Get(Session session, object? userdata) {
            var name = _nameCondition.GetString(session, userdata);
            
            return session.GetSlider(name);
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            LocalBuilder? temp = null;
            ctx.EmitLoadSession();
            
            ctx.EmitSwapOutCurrentCondition(ref temp, _nameCondition, FieldNameCondition);
            _nameCondition.Emit(ctx, typeof(string));
            ctx.EmitRevertCurrentCondition(temp);
            
            ctx.Il.Emit(OpCodes.Callvirt, MethodSessionGetSlider);
            ctx.EmitConvertTo(typeof(float), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => _nameCondition.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => typeof(float);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [_nameCondition];
    }
    
    private sealed class CounterAccessor(string name) : Condition {
        private Session.Counter? _valueCounter;
        private WeakReference<Session>? _lastSession;

        private static readonly MethodInfo MethodGetInt
            = typeof(CounterAccessor).GetMethod(nameof(GetCached), BindingFlags.Instance | BindingFlags.NonPublic)!;
        
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
            ctx.EmitLoadCurrentCondition<CounterAccessor>();
            ctx.EmitLoadSession();
            ctx.Il.Emit(OpCodes.Callvirt, MethodGetInt);
            ctx.EmitConvertTo(typeof(int), targetType);
        }

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [name];
    }
    
    private sealed class IndirectCounterAccessor(Condition nameCond) : Condition {
        private readonly Condition _nameCondition = nameCond;
        private static readonly FieldInfo FieldNameCondition = typeof(IndirectCounterAccessor).GetField(nameof(_nameCondition), BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo MethodSessionGetCounter = typeof(Session).GetMethod(nameof(Session.GetCounter), BindingFlags.Instance | BindingFlags.Public)!;

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
    
    private sealed class Empty : Condition {
        public override object Get(Session session, object? userdata) {
            return One;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            ctx.Il.EmitLoadConstAs(One, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        public override bool OnlyChecksFlags() => true;
    }
    
    private interface IInvertible {
        public Condition CreateInverted();
    }

    public abstract class Condition : ISavestatePersisted {
        internal static readonly object One = 1;
        internal static readonly object Zero = 0;

        protected object BoolToBoxedInt(bool b) => b ? One : Zero;
        
        public abstract object Get(Session session, object? userdata);

        protected virtual IEnumerable<object> GetArgsForDebugPrint() => [];

        protected internal virtual Type? ReturnType => null;
        
        protected internal bool ReturnTypeIsNumber => ReturnType == typeof(int) || ReturnType == typeof(float);

        private static readonly MethodInfo MethodGetT = typeof(Condition).GetMethod(nameof(Get), genericParameterCount: 1, BindingFlags.NonPublic | BindingFlags.Instance, null, [ typeof(Session), typeof(object) ], null)!;
        
        /// <summary>
        /// Emits IL code to execute this condition.
        /// The emitted code should assume its operating on an empty stack,
        /// and should leave exactly 1 value of type <paramref name="targetType"/> on top of the stack.
        ///
        /// Converting a value to <paramref name="targetType"/> is best done via calling <see cref="ConditionCompilationCtx.EmitConvertTo"/>.
        ///
        /// If this condition has any inner conditions nested within it, the best way to evaluate them is via:
        /// <code>
        ///     LocalBuilder? temp = null;
        ///     ctx.EmitSwapOutCurrentCondition(ref temp, nextCondition, ...);
        ///     nextCondition.Emit(ctx, type);
        ///     // ... potentially call EmitSwapOutCurrentCondition for other conditions ...
        ///     ctx.EmitRevertCurrentCondition(temp);
        ///     // ... do something with the value returned from nextCondition ...
        /// </code>
        ///
        /// Make sure to also override <see cref="UsesCurrentConditionLocalInEmit"/> if overriding this method.
        /// </summary>
        /// <param name="ctx">Context about the current compilation.</param>
        /// <param name="targetType">The type of value that should be left on the stack.</param>
        internal virtual void Emit(ConditionCompilationCtx ctx, Type targetType) {
            if (!UsesCurrentConditionLocalInEmit) {
                throw new Exception($"UsesCurrentConditionLocalInEmit is false, but CurrentCondition is being used by {GetType()}!");
            }
            ctx.EmitLoadCurrentCondition();
            ctx.EmitLoadSession();
            ctx.EmitLoadUserdata();
            ctx.Il.Emit(OpCodes.Callvirt, MethodGetT.MakeGenericMethod(targetType));
        }

        /// <summary>
        /// Whether the code emitted via <see cref="Emit"/> makes use of the CurrentCondition local variable.
        /// If true, additional boilerplate IL code needs to be emitted before evaluating this condition, so it's best to return false if possible.
        /// If this condition has any inner conditions nested within it, this method needs to return true if any of the child conditions return true. 
        /// </summary>
        internal virtual bool UsesCurrentConditionLocalInEmit => true;

        internal int GetInt(Session session, object? userdata = null) {
            return GetNumber<int>(session, userdata);
        }
        
        internal float GetFloat(Session session, object? userdata = null) {
            return GetNumber<float>(session, userdata);
        }

        internal T GetNumber<T>(Session session, object? userdata = null) where T : struct, INumber<T> {
            var obj = Get(session, userdata);

            return CoerceToNumber<T>(obj);
        }

        internal string GetString(Session session, object? userdata = null) {
            var obj = Get(session, userdata);
            if (obj is string str)
                return str;
            
            if (obj is IFormattable f)
                return f.ToString(null, CultureInfo.InvariantCulture);

            return obj.ToString() ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T Get<T>(Session session, object? userdata = null) {
            if (typeof(T) == typeof(bool))
                return (T)(object)Check(session, userdata);
            if (typeof(T) == typeof(int))
                return (T)(object)GetInt(session, userdata);
            if (typeof(T) == typeof(float))
                return (T)(object)GetFloat(session, userdata);
            if (typeof(T) == typeof(string))
                return (T)(object)GetString(session, userdata);
            if (typeof(T) == typeof(object))
                return (T)Get(session, userdata);

            object ret = Get(session, userdata);
            if (ret.GetType().IsAssignableTo(typeof(T))) {
                return (T) ret;
            }

            throw new ArgumentException($"Unsupported T for Session Expression: {typeof(T).FullName}");
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(object? userdata = null) => Check(FrostModule.GetCurrentLevel().Session, userdata);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(Session session, object? userdata = null) => CoerceToBool(Get(session, userdata));

        public bool Empty => this is Empty;

        public bool IsSimpleFlagCheck([NotNullWhen(true)] out string? checkedFlag) {
            if (this is FlagAccessor { Inverted: false, Flag: not null } f) {
                checkedFlag = f.Flag;
                return true;
            }

            checkedFlag = null;
            return false;
        }

        public virtual bool OnlyChecksFlags() => false;
        
        public static bool CoerceToBool(object obj) {
            return obj switch {
                bool b => b,
                int i => i != 0,
                float f => f != 0,
                null => false,
                _ => true,
            };
        }
        
        public static T CoerceToNumber<T>(object obj) where T : INumber<T> {
            if (obj is T t)
                return t;
            
            switch (obj) {
                case float f:
                    return T.CreateTruncating(f);
                case double f:
                    return T.CreateTruncating(f);
                case int f:
                    return T.CreateTruncating(f);
                case short f:
                    return T.CreateTruncating(f);
                case byte f:
                    return T.CreateTruncating(f);
            }
            
            NotificationHelper.Notify($"Can't convert Session Expression value '{obj}' [{obj?.GetType().Name ?? "null"}] to {typeof(T).Name}.\nReturning 0!");
            return T.Zero;
        }
        
        public sealed override string ToString() => ToStringIndented("");
        
        private string ToStringIndented(string indent) {
            var args = GetArgsForDebugPrint().ToArray();
            var builder = new StringBuilder($"{indent}{GetType().Name}(");

            if (args is []) {
                builder.Append(')');
            }
            
            for (int i = 0; i < args.Length; i++) {
                if (args[i] is not Condition innerCond) {
                    builder.Append($"{args[i]}{(i + 1 < args.Length ? "," : ")")}");
                } else {
                    var nextIndent = "  " + indent;
                    builder.Append($"\n{innerCond.ToStringIndented(nextIndent)}{(i + 1 < args.Length ? "," : $"\n{indent})")}");
                }
            }

            return builder.ToString();
        }
    }

    public static Condition GetCondition(this EntityData data, string name, string def = "")
        => GetConditionCore(data.Values, ExpressionContext.Default, name, def);
    
    public static Condition GetCondition(this EntityData data, ExpressionContext ctx, string name, string def = "")
        => GetConditionCore(data.Values, ctx, name, def);
    
    internal static SessionExpression<T> GetExpression<T>(this EntityData data, string name, string def = "")
        => new(GetConditionCore(data.Values, ExpressionContext.Default, name, def));
    
    public static Condition GetCondition(this BinaryPacker.Element data, string name, string def = "")
        => GetConditionCore(data.Attributes, ExpressionContext.Default, name, def);
    
    public static Condition GetCondition(this BinaryPacker.Element data, ExpressionContext ctx, string name, string def = "")
        => GetConditionCore(data.Attributes, ctx, name, def);

    private static Condition GetConditionCore(Dictionary<string, object>? dict, ExpressionContext ctx, string name, string def = "") {
        Condition? condition = null;
        if (dict?.TryGetValue(name, out var cond) ?? false) {
            switch (cond) {
                case Condition fullCondition:
                    condition = fullCondition;
                    break;
                case string str:
                    if (TryCreate(str, ctx, out condition)) {
                       // dict[name] = condition; // cache the parsed condition
                    }
                    break;
                // If an update converts a previously-number field into a session expression string field,
                // old .bins will still have numbers stored here and as such we should convert them.
                case int i:
                    return new ConstInt(i);
                case float f:
                    return new ConstFloat(f);
                case bool b:
                    return new ConstInt(b ? 1 : 0);
                default:
                    if (TryCreate(cond.ToString() ?? "", ctx, out condition)) {
                        // dict[name] = condition; // cache the parsed condition
                    }
                    break;
            }
        }

        if (condition is null && TryCreate(def, ctx, out condition)) {
            return condition;
        }
        
        condition ??= EmptyCondition;
        return condition;
    }
}

/// <summary>
/// A wrapper over a <see cref="ConditionHelper.Condition"/>, allowing for easily obtaining a specific return type.
/// </summary>
/// <typeparam name="T">The type returned from the expression</typeparam>
internal sealed class SessionExpression<T> {
    private readonly ConditionHelper.Condition? _condition;

    public SessionExpression(T constantValue) {
        IsConstant = true;
        ConstantValue = constantValue;
        IsNotEmpty = true;
    }
    
    public SessionExpression(ConditionHelper.Condition condition) {
        _condition = condition;
        if (condition is ConditionHelper.IConstCondition<T> constCond) {
            IsConstant = true;
            ConstantValue = constCond.Value;
        }

        IsNotEmpty = !condition.Empty;
    }
    
    public bool IsNotEmpty { get; }

    public bool IsConstant { get; }
    
    /// <summary>
    /// The constant value of this expression, default if this is not a constant expression.
    /// </summary>
    public T? ConstantValue { get; }
    
    public T Get(Scene scene) {
        return _condition is null ? ConstantValue! : _condition.Get<T>(scene.ToLevel().Session);
    }
    
    public T Get(Session session) {
        return _condition is null ? ConstantValue! : _condition.Get<T>(session);
    }
    
    public T Get(Session session, object userdata) {
        return _condition is null ? ConstantValue! : _condition.Get<T>(session, userdata);
    }
}

internal static class SessionExpressionExt {
    extension<T>(SessionExpression<T> e) where T : struct, INumber<T> {
        public bool CanBePositive => e.IsConstant ? e.ConstantValue > T.Zero : e.IsNotEmpty;
    }
}
