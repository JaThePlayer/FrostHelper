using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static FrostHelper.Helpers.ConditionHelper;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace FrostHelper.SessionExpressions;

public delegate bool FunctionCommandFactory(
    IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? result, 
    [NotNullWhen(false)] out string? errorMessage);

internal static class FunctionCommands {
    private static readonly Dictionary<string, FunctionCommandFactory> Registry = new() {
        ["min"] = MinCondition.TryCreate,
        ["max"] = MaxCondition.TryCreate,
        ["clamp"] = ClampCondition.TryCreate,
        ["abs"] = PureMathCondition.TryCreateIntOrFloat<AbsFunc<int>, AbsFunc<float>>,
        ["sin"] = PureMathCondition.TryCreateFloat<SinFunc>,
        ["cos"] = PureMathCondition.TryCreateFloat<CosFunc>,
        ["tan"] = PureMathCondition.TryCreateFloat<TanFunc>,
        ["dialog"] = DialogCondition.TryCreate,
        ["truncate"] = PureMathCondition.TryCreateFloat<TruncateFunc>,
        ["round"] = PureMathCondition.TryCreateFloat<RoundFunc>,
        ["vec"] = VecCondition.TryCreate,
        ["pow"] = PowerCondition.TryCreate,
    };

    public static void Register(string modName, string cmdName, Func<Session, object?, IReadOnlyList<object>, object> func) {
        var key = $"{modName}.{cmdName}";
        if (Registry.TryGetValue(key, out var existing)) {
            Logger.Warn("FrostHelper.ConditionHelper", $"Replacing function command '${key}'");
        }

        Registry[key] = CreateFactoryForCustomCommand(func);
    }

    internal static FunctionCommandFactory CreateFactoryForCustomCommand(Func<Session, object?, IReadOnlyList<object>, object> func) {
        return (IReadOnlyList<Condition> args, out Condition? result, out string? message) => {
            result = new ModFunctionCondition(args, func);
            message = null;
            return true;
        };
    }
    
    public static bool TryCreate(string name, IReadOnlyList<Condition> args, ExpressionContext ctx, [NotNullWhen(true)] out Condition? condition) {
        if (!ctx.FunctionCommands.TryGetValue(name, out var factory) && !Registry.TryGetValue(name, out factory)) {
            NotificationHelper.Notify($"Unknown Session Expression function: '{name}'");
            condition = null;
            return false;
        }

        if (!factory(args, out condition, out var errorMessage)) {
            NotificationHelper.Notify($"Failed to create Session Expression function: '{name}':\n{errorMessage}");
            condition = null;
            return false;
        }

        return true;
    }

    private sealed class LazyFunctionArgumentList(IReadOnlyList<Condition> args) : IReadOnlyList<object> {
        public Session Session { get; set; }
        public object? UserData { get; set; }
        
        private readonly object?[] _cache = new object[args.Count];

        public void Reset(Session session, object? userdata) {
            Array.Clear(_cache);
            Session = session;
            UserData = userdata;
        }
        
        public IEnumerator<object> GetEnumerator()
            => args.Select(x => x.Get(Session, UserData)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public int Count => args.Count;

        public object this[int index] 
            => args[index].Get(Session, UserData);
    }

    private sealed class ModFunctionCondition(IReadOnlyList<Condition> args, Func<Session, object?, IReadOnlyList<object>, object> func) 
        : FunctionCondition(args) {
        private readonly object[] _array = new object[args.Count];
        //private readonly LazyFunctionArgumentList _args = new(args);
        
        public override object Get(Session session, object? userdata) {
            for (int i = 0; i < args.Count; i++) {
                _array[i] = args[i].Get(session, userdata);
            }
            
            return func(session, userdata, _array);
            // TODO: test!
            //_args.Reset(session, userdata);
            //return func(session, userdata, _args);
        }
    }
    
    private interface IPureMathFunc<T> where T : struct, INumber<T> {
        public static abstract object Get(T x);
    }
    
    private struct SinFunc : IPureMathFunc<float> {
        public static object Get(float x) => float.Sin(x);
    }
    
    private struct CosFunc : IPureMathFunc<float> {
        public static object Get(float x) => float.Cos(x);
    }
    
    private struct TanFunc : IPureMathFunc<float> {
        public static object Get(float x) => float.Tan(x);
    }
    
    private struct TruncateFunc : IPureMathFunc<float> {
        public static object Get(float x) => float.Truncate(x);
    }
    
    private struct RoundFunc : IPureMathFunc<float> {
        public static object Get(float x) => float.Round(x);
    }
    
    private struct AbsFunc<T> : IPureMathFunc<T> where T : struct, INumber<T> {
        public static object Get(T x) => T.Abs(x);
    }

    private sealed class PureMathCondition<TNum, TOp>(Condition x) : FunctionCondition(x)
        where TNum : struct, INumber<TNum>
        where TOp : struct, IPureMathFunc<TNum> {
        
        public override object Get(Session session, object? userdata) {
            return TOp.Get(x.GetNumber<TNum>(session, userdata));
        }
    }

    private static class PureMathCondition {
        public static bool TryCreateIntOrFloat<TInt, TFloat>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TInt : struct, IPureMathFunc<int>
            where TFloat : struct, IPureMathFunc<float>
        {
            if (args is not [var only]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
            }

            if (only.ReturnType == typeof(int)) {
                return FunctionCondition.Ok(new PureMathCondition<int, TInt>(only), out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathCondition<float, TFloat>(only), out condition, out errorMessage);
        }
        
        public static bool TryCreateFloat<TFloat>(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
            [NotNullWhen(false)] out string? errorMessage) 
            where TFloat : struct, IPureMathFunc<float>
        {
            if (args is not [var only]) {
                return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new PureMathCondition<float, TFloat>(only), out condition, out errorMessage);
        }
    }

    private sealed class MinCondition(IEnumerable<Condition> x, Type type) : FunctionCondition(x) {
        private T Get<T>(Session session, object? userdata) where T : struct, INumber<T>, IMinMaxValue<T> {
            T min = T.MaxValue;
            foreach (var c in Conditions) {
                min = T.Min(c.GetNumber<T>(session, userdata), min);
            }

            return min;
        }

        protected internal override Type ReturnType => type;

        public override object Get(Session session, object? userdata) {
            if (ReturnType == typeof(int))
                return Get<int>(session, userdata);
            return Get<float>(session, userdata);
        }
        
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [_, ..]) {
                return TooFewArgs(args.Count, 1, out condition, out errorMessage);
            }

            if (args.All(x => x.ReturnType == typeof(int))) {
                return Ok(new MinCondition(args, typeof(int)), out condition, out errorMessage);
            }

            return Ok(new MinCondition(args, typeof(float)), out condition, out errorMessage);
        }
    }
    
    private sealed class MaxCondition(IEnumerable<Condition> x, Type type) : FunctionCondition(x) {
        private T Get<T>(Session session, object? userdata) where T : struct, INumber<T>, IMinMaxValue<T> {
            T max = T.MinValue;
            foreach (var c in Conditions) {
                max = T.Max(c.GetNumber<T>(session, userdata), max);
            }

            return max;
        }

        protected internal override Type ReturnType => type;

        public override object Get(Session session, object? userdata) {
            if (ReturnType == typeof(int))
                return Get<int>(session, userdata);
            return Get<float>(session, userdata);
        }
        
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [_, ..]) {
                return TooFewArgs(args.Count, 1, out condition, out errorMessage);
            }

            if (args.All(x => x.ReturnType == typeof(int))) {
                return Ok(new MaxCondition(args, typeof(int)), out condition, out errorMessage);
            }

            return Ok(new MaxCondition(args, typeof(float)), out condition, out errorMessage);
        }
    }
    
    private sealed class VecCondition(Condition x, Condition y) : FunctionCondition(x, y) {
        protected internal override Type ReturnType => typeof(Vector2);

        public override object Get(Session session, object? userdata) {
            return new Vector2(x.GetFloat(session, userdata), y.GetFloat(session, userdata));
        }
        
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [var x, var y]) {
                return ArgumentAmtMismatch(args.Count, 2, out condition, out errorMessage);
            }

            return Ok(new VecCondition(x, y), out condition, out errorMessage);
        }
    }
    
    private sealed class ClampCondition {
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [var x, var min, var max]) {
                return FunctionCondition.TooFewArgs(args.Count, 1, out condition, out errorMessage);
            }

            if (args.All(x => x.ReturnType == typeof(int))) {
                return FunctionCondition.Ok(new Impl<int>(x, min, max), out condition, out errorMessage);
            }

            return FunctionCondition.Ok(new Impl<float>(x, min, max), out condition, out errorMessage);
        }

        private sealed class Impl<T>(Condition x, Condition min, Condition max) : FunctionCondition(x, min, max) 
        where T : struct, INumber<T> {
            public override object Get(Session session, object? userdata) {
                var xVal = x.GetNumber<T>(session, userdata);
                var minVal = min.GetNumber<T>(session, userdata);
                var maxVal = max.GetNumber<T>(session, userdata);
                if (minVal > maxVal)
                    return T.Clamp(xVal, maxVal, minVal);
                return T.Clamp(xVal, minVal, maxVal);
            }
        }
    }

    private sealed class PowerCondition(Condition x, Condition y) : FunctionCondition(x, y) {
        protected internal override Type ReturnType => type;

        public override object Get(Session session, object? userdata) {
            if (ReturnType == typeof(int))
                return Get<int>(session, userdata);
            return Get<float>(session, userdata);
        }
        
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [var x, var y]) {
                return ArgumentAmtMismatch(args.Count, 2, out condition, out errorMessage);
            }

            return Ok(new PowerCondition(x, y), out condition, out errorMessage);
        }
    }

    private sealed class DialogCondition(Condition key) : FunctionCondition(key) {
        public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [var x]) {
                return TooFewArgs(args.Count, 1, out condition, out errorMessage);
            }
            
            return Ok(new DialogCondition(x), out condition, out errorMessage);
        }

        public override object Get(Session session, object? userdata) {
            var name = key.GetString(session, userdata);

            return Dialog.Clean(name);
        }

        protected internal override Type ReturnType => typeof(string);
    }
    
    internal abstract class FunctionCondition : Condition {
        protected readonly Condition[] Conditions;

        public FunctionCondition(params IEnumerable<Condition> conditions) {
            Conditions = conditions.ToArray();
        }

        protected override IEnumerable<object> GetArgsForDebugPrint() => Conditions;

        protected internal static bool ArgumentAmtMismatch(int received, int expected, 
            out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            
            condition = null;
            if (received > expected) {
                
                errorMessage = $"Too many arguments: {received}, expected: {expected}";
                return false;
            }

            if (received < expected) {
                errorMessage = $"Too few arguments: {received}, expected: {expected}";
                return false;
            }

            errorMessage = null;
            return true;
        }
        
        protected internal static bool TooFewArgs(int received, int expected, 
            out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            condition = null;
            if (received < expected) {
                errorMessage = $"Too few arguments: {received}, expected: {expected}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        protected internal static bool Ok(Condition condition, [NotNullWhen(true)] out Condition? retCond, [NotNullWhen(false)] out string? errorMessage) {
            retCond = condition;
            errorMessage = null;
            return true;
        }
    }
}