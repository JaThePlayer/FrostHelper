using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static FrostHelper.Helpers.ConditionHelper;

namespace FrostHelper.SessionExpressions;

internal static class FunctionCommands {
    public delegate bool FunctionDelegateFactory(IList<Condition> args, [NotNullWhen(true)] out Condition? result, [NotNullWhen(false)] out string? errorMessage);

    private static readonly Dictionary<string, FunctionDelegateFactory> Registry = new() {
        ["min"] = MinCondition.TryCreate,
        ["max"] = MaxCondition.TryCreate,
        ["abs"] = AbsCondition.TryCreate,
        ["sin"] = SinCondition.TryCreate,
    };
    
    public static bool TryCreate(string name, IList<Condition> args, [NotNullWhen(true)] out Condition? condition) {
        if (!Registry.TryGetValue(name, out var factory)) {
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


    private sealed class SinCondition(Condition x) : FunctionCondition(x) {
        public override object Get(Session session) {
            return float.Sin(x.GetFloat(session));
        }

        public static bool TryCreate(IList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [var only]) {
                return ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
            }

            return Ok(new SinCondition(only), out condition, out errorMessage);
        }
    }
    
    private sealed class AbsCondition(Condition x) : FunctionCondition(x) {
        public override object Get(Session session) {
            return float.Abs(x.GetFloat(session));
        }

        public static bool TryCreate(IList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [var only]) {
                return ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
            }

            if (only.ReturnType == typeof(int)) {
                return Ok(new AbsInt(only), out condition, out errorMessage);
            }

            return Ok(new AbsCondition(only), out condition, out errorMessage);
        }
        
        private sealed class AbsInt(Condition x) : FunctionCondition(x) {
            public override object Get(Session session) {
                return int.Abs(x.GetInt(session));
            }
        }
    }

    private sealed class MinCondition(IEnumerable<Condition> x, Type type) : FunctionCondition(x) {
        private T Get<T>(Session session) where T : struct, INumber<T>, IMinMaxValue<T> {
            T min = T.MinValue;
            foreach (var c in Conditions) {
                min = T.Min(c.GetNumber<T>(session), min);
            }

            return min;
        }

        protected internal override Type ReturnType => type;

        public override object Get(Session session) {
            if (ReturnType == typeof(int))
                return Get<int>(session);
            return Get<float>(session);
        }
        
        public static bool TryCreate(IList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
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
        private T Get<T>(Session session) where T : struct, INumber<T>, IMinMaxValue<T> {
            T max = T.MaxValue;
            foreach (var c in Conditions) {
                max = T.Max(c.GetNumber<T>(session), max);
            }

            return max;
        }

        protected internal override Type ReturnType => type;

        public override object Get(Session session) {
            if (ReturnType == typeof(int))
                return Get<int>(session);
            return Get<float>(session);
        }
        
        public static bool TryCreate(IList<Condition> args, [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            if (args is not [_, ..]) {
                return TooFewArgs(args.Count, 1, out condition, out errorMessage);
            }

            if (args.All(x => x.ReturnType == typeof(int))) {
                return Ok(new MaxCondition(args, typeof(int)), out condition, out errorMessage);
            }

            return Ok(new MaxCondition(args, typeof(float)), out condition, out errorMessage);
        }
    }
    
    internal abstract class FunctionCondition : Condition {
        public readonly Condition[] Conditions;

        public FunctionCondition(params IEnumerable<Condition> conditions) {
            Conditions = conditions.ToArray();
        }

        public override bool OnlyChecksFlags() => false;

        protected override IEnumerable<object> GetArgsForDebugPrint() => Conditions;

        protected static bool ArgumentAmtMismatch(int received, int expected, 
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
        
        protected static bool TooFewArgs(int received, int expected, 
            out Condition? condition, [NotNullWhen(false)] out string? errorMessage) {
            condition = null;
            if (received < expected) {
                errorMessage = $"Too few arguments: {received}, expected: {expected}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        protected static bool Ok(Condition condition, [NotNullWhen(true)] out Condition? retCond, [NotNullWhen(false)] out string? errorMessage) {
            retCond = condition;
            errorMessage = null;
            return true;
        }
    }
}