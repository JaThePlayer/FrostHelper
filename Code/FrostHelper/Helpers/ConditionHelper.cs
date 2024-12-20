using FrostHelper.ModIntegration;
using FrostHelper.SessionExpressions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace FrostHelper.Helpers;

public static class ConditionHelper {
    private static readonly Condition EmptyCondition = new Empty();

    internal static Condition CreateOrDefault(string txt, string defaultValue) {
        if (TryCreate(txt, out var cond))
            return cond;
        if (TryCreate(defaultValue, out cond))
            return cond;
        
        NotificationHelper.Notify($"Default condition is malformed, this is a Frost Helper bug!\n{defaultValue}\n{new StackTrace()}");
        return EmptyCondition;
    }
    
    internal static bool TryCreate(string str, [NotNullWhen(true)] out Condition? condition) {
        if (string.IsNullOrWhiteSpace(str)) {
            condition = EmptyCondition;
            return true;
        }

        if (AbstractExpression.TryParseCached(str, out var expr)) {
            return TryCreate(expr, out condition);
        }

        condition = null;
        return false;
    }

    private static bool TryCreate(AbstractExpression expr, [NotNullWhen(true)] out Condition? condition) {
        if (expr is { Operator: "!" or "#" or "$" or "@", Right: null, Left: { } unaryLeft }) {
            switch (expr.Operator, unaryLeft.StringValue)
            {
                case ("!", {} flagName):
                    condition = new FlagAccessor(flagName, inverted: true);
                    return true;
                case ("!", _) when TryCreate(unaryLeft, out var toInvert):
                    if (toInvert is IInvertible invertible)
                        condition = invertible.CreateInverted();
                    else
                        condition = new OperatorInvert(toInvert);
                    return true;
                case ("!", _):
                    NotificationHelper.Notify($"'!' operator with invalid operator: '{expr}'");
                    condition = null;
                    return false;
                case ("#", {} flagName):
                    condition = new CounterAccessor(flagName);
                    return true;
                case ("#", _):
                    NotificationHelper.Notify($"Unnecessary '#' operator: '{expr}'");
                    return TryCreate(unaryLeft, out condition);
                case ("@", {} sliderName): {
                    condition = new SliderAccessor(sliderName);
                    return true;
                }
                case ("@", _):
                    NotificationHelper.Notify($"Invalid use of the '@' operator: '{expr}'");
                    condition = null;
                    return false;
                case ("$", ['i', 'n', 'p', 'u', 't', '.', .. var rest]): {
                    return InputCommands.TryParseInput(rest, out condition);
                }
                case ("$", _):
                    // Try simple commands
                    if (SimpleCommands.Registry.TryGetValue(unaryLeft.StringValue ?? "", out var cond)) {
                        condition = cond;
                        return true;
                    }
                    
                    NotificationHelper.Notify($"Unknown use of the $ operator: {expr}");
                    condition = null;
                    return false;
            }
        }

        if (expr is { Operator: "$call", StringValue: { } funcName, Arguments: { } args }) {
            var argConds = new List<Condition>(args.Count);
            foreach (var argExpr in args) {
                if (!TryCreate(argExpr, out var argCond)) {
                    condition = null;
                    return false;
                }
                argConds.Add(argCond);
            }
            return FunctionCommands.TryCreate(funcName, argConds, out condition);
        }
        
        if (expr.StringValue is { } c) {
            if (expr.Operator is not null) {
                NotificationHelper.Notify($"Unknown operator: {expr.Operator} [in: {expr}]");
                condition = null;
                return false;
            }
            
            if (int.TryParse(c, CultureInfo.InvariantCulture, out var i)) {
                condition = new ConstInt(i);
                return true;
            }
            
            if (float.TryParse(c, CultureInfo.InvariantCulture, out var f)) {
                condition = new ConstFloat(f);
                return true;
            }

            condition = new FlagAccessor(c, false);
            return true;
        }

        if (expr is { Left: { } left, Right: { } right }) {
            if (!TryCreate(left, out var leftExpr)) {
                condition = null;
                return false;
            }
            if (!TryCreate(right, out var rightExpr)) {
                condition = null;
                return false;
            }

            condition = expr.Operator switch {
                "&&" => new OperatorAnd(leftExpr, rightExpr),
                "||" => new OperatorOr(leftExpr, rightExpr),
                "&" => new OperatorBitwiseAnd(leftExpr, rightExpr),
                "|" => new OperatorBitwiseOr(leftExpr, rightExpr),
                "+" => new OperatorAdd(leftExpr, rightExpr),
                "-" => new OperatorSub(leftExpr, rightExpr),
                "*" => new OperatorMul(leftExpr, rightExpr),
                "/" => new OperatorDiv(leftExpr, rightExpr),
                "//" => new OperatorDivFloat(leftExpr, rightExpr),
                "%" => new OperatorModulo(leftExpr, rightExpr),
                "<" => new OperatorLt(leftExpr, rightExpr),
                ">" => new OperatorGt(leftExpr, rightExpr),
                "==" => new OperatorEq(leftExpr, rightExpr),
                "!=" => new OperatorNe(leftExpr, rightExpr),
                ">=" => new OperatorGte(leftExpr, rightExpr),
                "<=" => new OperatorLte(leftExpr, rightExpr),
                _ => null
            };

            if (condition is null) {
                NotificationHelper.Notify($"Unknown operator: {expr.Operator}");
                return false;
            }

            return true;
        }
        
        NotificationHelper.Notify($"Couldn't parse: {expr}");
        condition = null;
        return false;
    }

    private sealed class OperatorAnd(Condition a, Condition b) : Condition {
        public override object Get(Session session) {
            return CoerceToBool(a.Get(session)) && CoerceToBool(b.Get(session)) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [a, b];
    }
    
    private sealed class OperatorOr(Condition a, Condition b) : Condition {
        public override object Get(Session session) {
            return CoerceToBool(a.Get(session)) || CoerceToBool(b.Get(session)) ? 1 : 0;
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
    }
    
    private sealed class OperatorSub(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            return a - b;
        }
    }
    
    private sealed class OperatorMul(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
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
    }
    
    private sealed class OperatorDivFloat(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            if (T.IsZero(b)) {
                return 0f;
            }
            
            return float.CreateTruncating(a) / float.CreateTruncating(b);
        }

        protected internal override Type ReturnType => typeof(float);
    }
    
    private sealed class OperatorModulo(Condition a, Condition b) : MathOperator(a, b) {
        protected override object Perform<T>(T a, T b) {
            return a % b;
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

        protected override object Operate(object a, object b) {
            return (a, b) switch {
                (int ai, int bi) => Perform(ai, bi),
                (float ai, float bi) => Perform(ai, bi),
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
            return null;
        }
    }
    
    private abstract class BinaryOperator(Condition condA, Condition condB) : Condition {
        public override object Get(Session session) {
            var a = condA.Get(session);
            var b = condB.Get(session);

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
        public override object Get(Session session) {
            return CoerceToBool(x.Get(session)) ? 0 : 1;
        }
        
        public override bool OnlyChecksFlags() => x.OnlyChecksFlags();
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [x];
    }

    private sealed class ConstInt(int x) : Condition {
        public override object Get(Session session) => x;
        
        public override bool OnlyChecksFlags() => true;
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [x];
    }
    
    private sealed class ConstFloat(float x) : Condition {
        public override object Get(Session session) => x;
        
        public override bool OnlyChecksFlags() => true;
        
        protected internal override Type ReturnType => typeof(float);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [x];
    }

    private sealed class FlagAccessor(string name, bool inverted) : Condition, IInvertible {
        public string Flag => name;
        public bool Inverted => inverted;
        
        public override bool OnlyChecksFlags() => true;
        
        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [Inverted ? $"!{name}" : name];
        
        public Condition CreateInverted() {
            return new FlagAccessor(Flag, !Inverted);
        }
        
        public override object Get(Session session) {
            return session.GetFlag(name) != inverted ? 1 : 0;
        }
    }

    private sealed class PropertyAccessor(PropertyInfo prop, object? target) : Condition {
        // todo: MethodInvoker in .net8+
        private FastReflectionHelper.FastInvoker? _invoker;
        
        public override object Get(Session session) {
            _invoker ??= prop.GetGetMethod()!.GetFastInvoker();
            
            return _invoker(target) ?? 0;
        }

        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => prop.PropertyType;

        protected override IEnumerable<object> GetArgsForDebugPrint() => [prop.Name];
    }
    
    private sealed class SliderAccessor(string name) : Condition {
        private WeakReference<Session.Slider>? _slider;
        private WeakReference<Session>? _lastSession;
        
        public override object Get(Session session) {
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

        public override bool OnlyChecksFlags() => false;
    }
    
    private sealed class CounterAccessor(string name) : Condition {
        private Session.Counter? _valueCounter;
        private WeakReference<Session>? _lastSession;
        
        public override object Get(Session session) {
            if ((_lastSession?.TryGetTarget(out var last) ?? false) && last != session) {
                _valueCounter = null;
                _lastSession = null;
            }

            _lastSession ??= new WeakReference<Session>(session);
            _valueCounter ??= session.GetCounterObj(name);
            
            return _valueCounter.Value;
        }
        
        public override bool OnlyChecksFlags() => false;

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [name];
    }
    
    private sealed class Empty : Condition {
        public override object Get(Session session) {
            return 1;
        }

        public override bool OnlyChecksFlags() => true;
    }
    
    private interface IInvertible {
        public Condition CreateInverted();
    }

    public abstract class Condition : ISavestatePersisted {
        public abstract object Get(Session session);

        protected virtual IEnumerable<object> GetArgsForDebugPrint() => [];

        protected internal virtual Type? ReturnType => null;

        internal int GetInt(Session session) {
            return GetNumber<int>(session);
        }
        
        internal float GetFloat(Session session) {
            return GetNumber<float>(session);
        }

        internal T GetNumber<T>(Session session) where T : struct, INumber<T> {
            var obj = Get(session);

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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check() => Check(FrostModule.GetCurrentLevel().Session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(Session session) => CoerceToBool(Get(session));

        public bool Empty => this is Empty;

        public bool IsSimpleFlagCheck([NotNullWhen(true)] out string? checkedFlag) {
            if (this is FlagAccessor { Inverted: false } f) {
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

    public static Condition GetCondition(this EntityData data, string name, string def = "") {
        return GetConditionCore(data.Values, name, def);
    }
    
    public static Condition GetCondition(this BinaryPacker.Element data, string name, string def = "") {
        return GetConditionCore(data.Attributes, name, def);
    }

    private static Condition GetConditionCore(Dictionary<string, object> dict, string name, string def = "") {
        Condition? condition = null;
        if (dict.TryGetValue(name, out var cond)) {
            switch (cond) {
                case Condition fullCondition:
                    condition = fullCondition;
                    break;
                case string str:
                    if (TryCreate(str, out condition)) {
                        dict[name] = condition; // cache the parsed condition
                    }
                    break;
            }
        }

        if (condition is null && TryCreate(def, out condition)) {
            return condition;
        }
        
        condition ??= EmptyCondition;
        return condition;
    }
}
