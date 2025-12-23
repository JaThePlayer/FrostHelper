using FrostHelper.ModIntegration;
using FrostHelper.SessionExpressions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Vector2 = Microsoft.Xna.Framework.Vector2;

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
                    BinOpExpression.Operators.BitwiseAnd => new OperatorBitwiseAnd(leftExpr, rightExpr),
                    BinOpExpression.Operators.BitwiseOr => new OperatorBitwiseOr(leftExpr, rightExpr),
                    BinOpExpression.Operators.Add => new OperatorAdd(leftExpr, rightExpr),
                    BinOpExpression.Operators.Sub => new OperatorSub(leftExpr, rightExpr),
                    BinOpExpression.Operators.Mul => new OperatorMul(leftExpr, rightExpr),
                    BinOpExpression.Operators.Div => new OperatorDiv(leftExpr, rightExpr),
                    BinOpExpression.Operators.DivFloat => new OperatorDivFloat(leftExpr, rightExpr),
                    BinOpExpression.Operators.Modulo => new OperatorModulo(leftExpr, rightExpr),
                    BinOpExpression.Operators.Lt => new OperatorLt(leftExpr, rightExpr),
                    BinOpExpression.Operators.Gt => new OperatorGt(leftExpr, rightExpr),
                    BinOpExpression.Operators.Eq => new OperatorEq(leftExpr, rightExpr),
                    BinOpExpression.Operators.Ne => new OperatorNe(leftExpr, rightExpr),
                    BinOpExpression.Operators.Ge => new OperatorGte(leftExpr, rightExpr),
                    BinOpExpression.Operators.Le => new OperatorLte(leftExpr, rightExpr),
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

    private sealed class StringInterpolationOperator(List<Condition> args) : Condition {
        private readonly StringBuilder _stringBuilder = new();
        
        public override object Get(Session session, object? userdata) {
            var builder = _stringBuilder;
            
            foreach (var arg in args) {
                var obj = arg.Get(session, userdata);
                if (obj is string str)
                    builder.Append(str);
                else
                    builder.Append(CultureInfo.InvariantCulture, $"{obj}");
            }
            
            var ret = builder.ToString();
            builder.Clear();
            return ret;
        }

        protected internal override Type ReturnType => typeof(string);

        protected override IEnumerable<object> GetArgsForDebugPrint() => args;
    }

    private sealed class OperatorAnd(Condition a, Condition b) : Condition {
        public override object Get(Session session, object? userdata) {
            return CoerceToBool(a.Get(session, userdata)) && CoerceToBool(b.Get(session, userdata)) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [a, b];
    }
    
    private sealed class OperatorOr(Condition a, Condition b) : Condition {
        public override object Get(Session session, object? userdata) {
            return CoerceToBool(a.Get(session, userdata)) || CoerceToBool(b.Get(session, userdata)) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [a, b];
    }
    
    private sealed class OperatorBitwiseOr(Condition a, Condition b) : BitwiseOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            return a | b;
        }
    }
    
    private sealed class OperatorBitwiseAnd(Condition a, Condition b) : BitwiseOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            return a & b;
        }
    }

    private abstract class BitwiseOperator(Condition condA, Condition condB) : BinaryOperator(condA, condB) {
        protected override object Operate(object a, object b) {
            return (a, b) switch {
                (int aInt, int bInt) => Perform(aInt, bInt),
                (float aF, float bF) => Perform((int) aF, (int) bF),
                _ => LogIncomparableTypes(a, b)
            };
        }
        
        protected abstract object Perform<T>(T a, T b) where T : IBinaryNumber<T>;
        
        private object LogIncomparableTypes(object a, object b) {
            NotificationHelper.Notify($"Can't perform bitwise operations on objects of types: {a.GetType()} and {b.GetType()}. Result will always be 0!");
            return 0;
        }
    }
    
    private sealed class OperatorAdd(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            return a + b;
        }

        protected override object Perform(Vector2 a, float b) {
            return new Vector2(a.X + b, a.Y + b);
        }

        protected override object Perform(Vector2 a, Vector2 b) {
            return a + b;
        }
    }
    
    private sealed class OperatorSub(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            return a - b;
        }
        
        protected override object Perform(Vector2 a, float b) {
            return new Vector2(a.X - b, a.Y - b);
        }

        protected override object Perform(Vector2 a, Vector2 b) {
            return a - b;
        }
    }
    
    private sealed class OperatorMul(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            return a * b;
        }
        
        protected override object Perform(Vector2 a, float b) {
            return a * b;
        }

        protected override object Perform(Vector2 a, Vector2 b) {
            return a * b;
        }
    }
    
    private sealed class OperatorDiv(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            if (T.IsZero(b)) {
                return T.Zero;
            }
            return a / b;
        }
        
        protected override object Perform(Vector2 a, float b) {
            return a / b;
        }

        protected override object Perform(Vector2 a, Vector2 b) {
            return a / b;
        }
    }
    
    private sealed class OperatorDivFloat(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            if (T.IsZero(b)) {
                return 0f;
            }
            
            return float.CreateTruncating(a) / float.CreateTruncating(b);
        }
        
        protected override object Perform(Vector2 a, float b) {
            return a / b;
        }

        protected override object Perform(Vector2 a, Vector2 b) {
            return a / b;
        }

        protected internal override Type? ReturnType {
            get {
                var def = base.ReturnType;
                return def == typeof(int) ? typeof(float) : def;
            }
        }
    }
    
    private sealed class OperatorModulo(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            return a % b;
        }
        
        protected override object Perform(Vector2 a, float b) {
            return new Vector2(a.X % b, a.Y % b);
        }

        protected override object Perform(Vector2 a, Vector2 b) {
            return new Vector2(a.X % b.X, a.Y % b.Y);
        }
    }
    
    private sealed class OperatorEq(Condition a, Condition b) : ComparisonOperator(a, b) {
        protected override bool Compare<T>(T a, T b) {
            return a == b;
        }
    }
    
    private sealed class OperatorNe(Condition a, Condition b) : ComparisonOperator(a, b) {
        protected override bool Compare<T>(T a, T b) {
            return a != b;
        }
    }
    
    private sealed class OperatorGt(Condition a, Condition b) : ComparisonOperator(a, b) {
        protected override bool Compare<T>(T a, T b) {
            return a > b;
        }
    }
    
    private sealed class OperatorLt(Condition a, Condition b) : ComparisonOperator(a, b) {
        protected override bool Compare<T>(T a, T b) {
            return a < b;
        }
    }
    
    private sealed class OperatorGte(Condition a, Condition b) : ComparisonOperator(a, b) {
        protected override bool Compare<T>(T a, T b) {
            return a >= b;
        }
    }
    
    private sealed class OperatorLte(Condition a, Condition b) : ComparisonOperator(a, b) {
        protected override bool Compare<T>(T a, T b) {
            return a <= b;
        }
    }

    private abstract class ComparisonOperator(Condition condA, Condition condB) : BinaryOperator(condA, condB) {
        protected abstract bool Compare<T>(T a, T b) where T : IComparisonOperators<T, T, bool>;

        protected override object Operate(object a, object b) {
            return (a, b) switch {
                (int ai, int bi) => Compare(ai, bi),
                (float ai, float bi) => Compare(ai, bi),
                (string ai, string bi) => ai == bi,
                _ => LogIncomparableTypes(a, b)
            };
        }
        
        private object LogIncomparableTypes(object a, object b) {
            NotificationHelper.Notify($"Can't compare objects of types: {a.GetType()} and {b.GetType()}. Result will always be 0!");
            return 0;
        }

        protected internal override Type ReturnType => typeof(int);
    }

    private abstract class MathOperator(Condition condA, Condition condB) : BinaryOperator(condA, condB) {
        protected abstract object Perform<T>(T a, T b) where T : INumber<T>;
        
        protected abstract object Perform(Vector2 a, float b);
        
        protected abstract object Perform(Vector2 a, Vector2 b);

        protected override object Operate(object a, object b) {
            return (a, b) switch {
                (int ai, int bi) => Perform(ai, bi),
                (float ai, float bi) => Perform(ai, bi),
                (float bi, Vector2 v2) => Perform(v2, bi),
                (int bi, Vector2 v2) => Perform(v2, bi),
                (Vector2 v2, int bi) => Perform(v2, bi),
                (Vector2 v2, float bi) => Perform(v2, bi),
                (Vector2 v2, Vector2 bi) => Perform(v2, bi),
                _ => LogIncomparableTypes(a, b)
            };
        }

        private object LogIncomparableTypes(object a, object b) {
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
            return null;
        }
    }
    
    private abstract class BinaryOperator(Condition condA, Condition condB) : Condition {
        public override object Get(Session session, object? userdata) {
            var a = condA.Get(session, userdata);
            var b = condB.Get(session, userdata);

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
        
        public override bool OnlyChecksFlags() => condA.OnlyChecksFlags() && condB.OnlyChecksFlags();
        
        protected abstract object Operate(object a, object b);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [condA, condB];
    }

    private sealed class OperatorInvert(Condition x) : Condition {
        public override object Get(Session session, object? userdata) {
            return CoerceToBool(x.Get(session, userdata)) ? 0 : 1;
        }
        
        public override bool OnlyChecksFlags() => x.OnlyChecksFlags();
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [x];
    }

    private sealed class ConstInt(int x) : Condition {
        private readonly object _boxed = x;
        
        public override object Get(Session session, object? userdata) => _boxed;
        
        public override bool OnlyChecksFlags() => true;
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [ _boxed ];
    }
    
    private sealed class ConstFloat(float x) : Condition {
        private readonly object _boxed = x;
        
        public override object Get(Session session, object? userdata) => _boxed;
        
        public override bool OnlyChecksFlags() => true;
        
        protected internal override Type ReturnType => typeof(float);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [x];
    }
    
    private sealed class ConstString(string x) : Condition {
        public string Value => x;
        
        public override object Get(Session session, object? userdata) => x;
        
        public override bool OnlyChecksFlags() => true;
        
        protected internal override Type ReturnType => typeof(string);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [x];
    }

    private sealed class FlagAccessor(Condition nameCond, bool inverted) : Condition, IInvertible {
        public string? Flag => nameCond is ConstString c ? c.Value : null;
        
        public bool Inverted => inverted;
        
        public override bool OnlyChecksFlags() => nameCond.OnlyChecksFlags();
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [Inverted ? $"!{Flag ?? nameCond.ToString()}" : Flag ?? nameCond.ToString()];
        
        public Condition CreateInverted() {
            return new FlagAccessor(nameCond, !Inverted);
        }
        
        public override object Get(Session session, object? userdata) {
            var flag = Flag ?? nameCond.GetString(session, userdata);
            return session.GetFlag(flag) != inverted ? One : Zero;
        }
    }

    private sealed class PropertyAccessor(PropertyInfo prop, object? target) : Condition {
        // todo: MethodInvoker in .net8+
        private FastReflectionHelper.FastInvoker? _invoker;
        
        public override object Get(Session session, object? userdata) {
            _invoker ??= prop.GetGetMethod()!.GetFastInvoker();
            
            return _invoker(target) ?? Zero;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => prop.PropertyType;

        protected override IEnumerable<object> GetArgsForDebugPrint() => [prop.Name];
    }
    
    private sealed class SliderAccessor(string name) : Condition {
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

        protected internal override Type ReturnType => typeof(float);
    }
    
    private sealed class IndirectSliderAccessor(Condition nameCond) : Condition {
        public override object Get(Session session, object? userdata) {
            var name = nameCond.GetString(session, userdata);
            
            return session.GetSlider(name);
        }
        
        protected internal override Type ReturnType => typeof(float);
    }
    
    private sealed class CounterAccessor(string name) : Condition {
        private Session.Counter? _valueCounter;
        private WeakReference<Session>? _lastSession;
        
        public override object Get(Session session, object? userdata) {
            if ((_lastSession?.TryGetTarget(out var last) ?? false) && last != session) {
                _valueCounter = null;
                _lastSession = null;
            }

            _lastSession ??= new WeakReference<Session>(session);
            _valueCounter ??= session.GetCounterObj(name);
            
            return _valueCounter.Value;
        }

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [name];
    }
    
    private sealed class IndirectCounterAccessor(Condition nameCond) : Condition {
        public override object Get(Session session, object? userdata) {
            var name = nameCond.GetString(session, userdata);
            
            return session.GetCounter(name);
        }

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [nameCond];
    }
    
    private sealed class Empty : Condition {
        public override object Get(Session session, object? userdata) {
            return One;
        }

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

        internal int GetInt(Session session, object? userdata = null) {
            return GetNumber<int>(session, userdata);
        }
        
        internal float GetFloat(Session session, object? userdata = null) {
            return GetNumber<float>(session, userdata);
        }

        internal T GetNumber<T>(Session session, object? userdata = null) where T : struct, INumber<T> {
            var obj = Get(session, userdata);

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
            }
        }

        if (condition is null && TryCreate(def, ctx, out condition)) {
            return condition;
        }
        
        condition ??= EmptyCondition;
        return condition;
    }
}
