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
            var ret = TryCreate(expr, ctx, out condition);
            condition?.SourceText = str;
            return ret;
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
                        condition = nameCond is ConstString { Value: var n } ? new CounterAccessorCondition(n) : new IndirectCounterAccessor(nameCond);
                        return true;
                    case GetSessionVariableExpression.Types.Slider:
                        condition = nameCond is ConstString { Value: var sn } ? new SliderAccessorCondition(sn) : new IndirectSliderAccessor(nameCond);
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
            case FieldAccessExpression { Name: var fieldName, ObjectExpression: var objectExpression, Arguments: var arguments }: {
                if (!TryCreate(objectExpression, ctx, out var objExpr)) {
                    condition = null;
                    return false;
                }

                if (arguments is { }) {
                    List<Condition> args = [];
                    foreach (var argExpr in arguments) {
                        if (!TryCreate(argExpr, ctx, out var arg)) {
                            condition = null;
                            return false;
                        } 
                        
                        args.Add(arg);
                    }
                    condition = InstanceFunctionCommands.Create(fieldName, objExpr, args, ctx);
                    return true;
                }

                condition = FieldAccessCommands.Create(fieldName, objExpr, ctx);
                return true;
            }
        }

        NotificationHelper.Notify($"Couldn't parse: {expr}");
        condition = null;
        return false;
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
        private readonly Condition _innerCondition = x;
        private static readonly FieldInfo FieldInnerCondition = typeof(OperatorInvert).GetField(nameof(_innerCondition), BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        public override object Get(Session session, object? userdata) {
            return CoerceToBool(_innerCondition.Get(session, userdata)) ? Zero : One;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            LocalBuilder? temp = null;
            ctx.Il.EmitSwapOutCurrentCondition(ref temp, ctx, _innerCondition, FieldInnerCondition);
            _innerCondition.Emit(ctx, typeof(bool));
            ctx.Il.EmitRevertCurrentCondition(temp, ctx);
            
            ctx.Il.Emit(OpCodes.Ldnull);
            ctx.Il.Emit(OpCodes.Ceq);
            ctx.EmitConvertTo(typeof(bool), targetType);
        }

        public override bool OnlyChecksFlags() => _innerCondition.OnlyChecksFlags();
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [_innerCondition];
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
        internal string? SourceText { get; set; }
        
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
            if (ret.GetType() == typeof(T) || ret.GetType().IsAssignableTo(typeof(T))) {
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
    private readonly CompiledCondition<T>? _condition;

    public SessionExpression(T constantValue) {
        IsConstant = true;
        ConstantValue = constantValue;
        IsNotEmpty = true;
    }
    
    public SessionExpression(ConditionHelper.Condition condition) {
        if (condition is IConstCondition<T> constCond) {
            IsConstant = true;
            ConstantValue = constCond.Value;
        } else {
            _condition = CompiledCondition<T>.GetFor(condition);
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
        return _condition is null ? ConstantValue! : _condition.Get(scene.ToLevel().Session, null);
    }
    
    public T Get(Session session) {
        return _condition is null ? ConstantValue! : _condition.Get(session, null);
    }
    
    public T Get(Session session, object userdata) {
        return _condition is null ? ConstantValue! : _condition.Get(session, userdata);
    }
}

internal static class SessionExpressionExt {
    extension<T>(SessionExpression<T> e) where T : struct, INumber<T> {
        public bool CanBePositive => e.IsConstant ? e.ConstantValue > T.Zero : e.IsNotEmpty;
    }
}
