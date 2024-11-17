using FrostHelper.ModIntegration;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

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
        if (expr is { Operator: "!" or "#" or "$", Right: null, Left: { } unaryLeft }) {
            switch (expr.Operator, unaryLeft.StringValue)
            {
                case ("!", {} flagName):
                    condition = new FlagAccessor(flagName, inverted: true);
                    return true;
                case ("!", _) when TryCreate(unaryLeft, out var toInvert):
                    condition = new OperatorInvert(toInvert);
                    return true;
                case ("!", _):
                    condition = null;
                    return false;
                case ("#", {} flagName):
                    condition = new CounterAccessor(flagName);
                    return true;
                case ("#", _):
                    NotificationHelper.Notify($"Unnecessary # operator: {expr}");
                    return TryCreate(unaryLeft, out condition);
                case ("$", "deathsHere"):
                    condition = new DeathsAccessor(inCurrentLevel: true);
                    return true;
                case ("$", "deaths"):
                    condition = new DeathsAccessor(inCurrentLevel: false);
                    return true;
                case ("$", "hasGolden"):
                    condition = new HasGoldenAccessor();
                    return true;
                case ("$", "restartedFromGolden"):
                    condition = new RestartedFromGoldenAccessor();
                    return true;
                case ("$", "coreMode"):
                    condition = new CoreModeAccessor();
                    return true;
                case ("$", _):
                    NotificationHelper.Notify($"Unknown use of the $ operator: {expr}");
                    condition = null;
                    return false;
            }
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
        public override int Get(Session session) {
            return (a.Get(session) != 0 && b.Get(session) != 0) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorBitwiseAnd(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) & b.Get(session);
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorOr(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return (a.Get(session) != 0 || b.Get(session) != 0) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorBitwiseOr(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) | b.Get(session);
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorAdd(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) + b.Get(session);
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorSub(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) - b.Get(session);
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorMul(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) * b.Get(session);
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorDiv(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) / b.Get(session);
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorModulo(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) % b.Get(session);
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorEq(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) == b.Get(session) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorNe(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) != b.Get(session) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorGt(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) > b.Get(session) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorLt(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) < b.Get(session) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorGte(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) >= b.Get(session) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }
    
    private sealed class OperatorLte(Condition a, Condition b) : Condition {
        public override int Get(Session session) {
            return a.Get(session) <= b.Get(session) ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => a.OnlyChecksFlags() && b.OnlyChecksFlags();
    }

    private sealed class OperatorInvert(Condition x) : Condition {
        public override int Get(Session session) {
            return x.Get(session) != 0 ? 0 : 1;
        }
        
        public override bool OnlyChecksFlags() => x.OnlyChecksFlags();
    }

    private sealed class ConstInt(int x) : Condition {
        public override int Get(Session session) => x;
        
        public override bool OnlyChecksFlags() => true;
    }

    private sealed class FlagAccessor(string name, bool inverted) : Condition {
        public string Flag => name;
        public bool Inverted => inverted;
        
        public override int Get(Session session) {
            return session.GetFlag(name) != inverted ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => true;
    }
    
    private sealed class DeathsAccessor(bool inCurrentLevel) : Condition {
        public override int Get(Session session) {
            return inCurrentLevel ? session.DeathsInCurrentLevel : session.Deaths;
        }
        
        public override bool OnlyChecksFlags() => false;
    }
    
    private sealed class HasGoldenAccessor : Condition {
        public override int Get(Session session) {
            return session.GrabbedGolden ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => false;
    }
    
    private sealed class RestartedFromGoldenAccessor : Condition {
        public override int Get(Session session) {
            return session.RestartedFromGolden ? 1 : 0;
        }
        
        public override bool OnlyChecksFlags() => false;
    }
    
    private sealed class CoreModeAccessor : Condition {
        public override int Get(Session session) {
            return (int)session.CoreMode;
        }
        
        public override bool OnlyChecksFlags() => false;
    }
    
    private sealed class CounterAccessor(string name) : Condition {
        private Session.Counter? _valueCounter;
        private WeakReference<Session>? _lastSession;
        
        public override int Get(Session session) {
            if ((_lastSession?.TryGetTarget(out var last) ?? false) && last != session) {
                _valueCounter = null;
                _lastSession = null;
            }

            _lastSession ??= new WeakReference<Session>(session);
            _valueCounter ??= session.GetCounterObj(name);
            
            return _valueCounter.Value;
        }
        
        public override bool OnlyChecksFlags() => false;
    }
    
    private sealed class Empty : Condition {
        public override int Get(Session session) {
            return 1;
        }

        public override bool OnlyChecksFlags() => true;
    }

    public abstract class Condition : ISavestatePersisted {
        public abstract int Get(Session session);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check() => Check(FrostModule.GetCurrentLevel().Session);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(Session session) => Get(session) != 0;

        public bool Empty => this is Empty;

        public bool IsSimpleFlagCheck([NotNullWhen(true)] out string? checkedFlag) {
            if (this is FlagAccessor { Inverted: false } f) {
                checkedFlag = f.Flag;
                return true;
            }

            checkedFlag = null;
            return false;
        }

        public abstract bool OnlyChecksFlags();
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
