using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FrostHelper.SessionExpressions;

internal static class InstanceFunctionCommands {
    public delegate bool InstanceFunctionCommandFactory(
        ConditionHelper.Condition field, IReadOnlyList<ConditionHelper.Condition> args, [NotNullWhen(true)] out ConditionHelper.Condition? result, 
        [NotNullWhen(false)] out string? errorMessage);
    
    internal static readonly Dictionary<(Type, string), InstanceFunctionCommandFactory> Functions = new() {
        [(typeof(object), "str")] = OneArgInstanceFunc<object, string, string, Str>.TryCreate,
        
        [(typeof(string), "match")] = OneArgInstanceFunc<string, string, int, StringMatch>.TryCreate,
    };
    
    internal static ConditionHelper.Condition Create(string functionName, ConditionHelper.Condition target, IReadOnlyList<ConditionHelper.Condition> arguments, ExpressionContext ctx) {
        if (target.ReturnType is { } knownType && GetFactory(knownType, functionName, ctx) is { } factory) {
            if (!factory(target, arguments, out var condition, out var errorMessage)) {
                NotificationHelper.Notify($"Failed to create Session Expression function: '{functionName}', called on '{knownType}':\n{errorMessage}");
                return new ConstInt(0);
            }

            return condition;
        }
        
        return new DynamicInstanceFunction(functionName, target, arguments, ctx);
    }

    internal static InstanceFunctionCommandFactory? GetFactory(Type? type, string functionName, ExpressionContext ctx) {
        var currentType = type;
        while (currentType is not null) {
            if (Functions.TryGetValue((currentType, functionName), out var accessor)) {
                if (currentType != type) {
                    Functions[(type!, functionName)] = accessor;
                }
                return accessor;
            }

            foreach (var interfaceType in currentType.GetInterfaces()) {
                if (Functions.TryGetValue((interfaceType, functionName), out accessor)) {
                    if (currentType != type) {
                        Functions[(type!, functionName)] = accessor;
                    }
                    return accessor;
                }
            }

            currentType = currentType.BaseType;
        }

        return null;
    }
    

    internal interface IOneArgFunc<in TField, in TArg, out TResult> {
        public static abstract TResult Invoke(TField field, TArg arg);
    }

    internal sealed class DynamicInstanceFunction(string functionName, ConditionHelper.Condition target, IReadOnlyList<ConditionHelper.Condition> arguments, ExpressionContext ctx) : ConditionHelper.Condition {
        public override object Get(Session session, object? userdata) {
            var obj = target.Get(session, userdata);
            if (_cache.TryGetValue(obj.GetType(), out var cached))
                return cached.Get(session, userdata);

            var factory = GetFactory(obj.GetType(), functionName, ctx);
            string? errorMessage = null;
            if (factory is null || !factory(target, arguments, out var condition, out errorMessage)) {
                NotificationHelper.Notify($"Failed to create Session Expression function: '{functionName}', called on '{obj.GetType()}':\n{errorMessage ?? "function not found"}");
                _cache[obj.GetType()] = new ConstInt(0);
                return Zero;
            }
            
            _cache[obj.GetType()] = condition;
            return condition.Get(session, userdata);
        }
        
        private readonly Dictionary<Type, ConditionHelper.Condition> _cache = [];
    }

    internal sealed class OneArgInstanceFunc<TField, TArg, TResult, TOp> : ConditionHelper.Condition
        where TOp : IOneArgFunc<TField, TArg, TResult> {

        private readonly ConditionHelper.Condition _field;
        private readonly ConditionHelper.Condition _arg;

        public static bool TryCreate(ConditionHelper.Condition field, IReadOnlyList<ConditionHelper.Condition> args,
            [NotNullWhen(true)] out ConditionHelper.Condition? result,
            [NotNullWhen(false)] out string? errorMessage) {
            result = null;
            errorMessage = null;

            if (args is not [{ } onlyArg]) {
                return FunctionCommands.FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out result, out errorMessage);
            }
                
            result = new OneArgInstanceFunc<TField, TArg, TResult, TOp>(field, onlyArg);
            return true;
        }

        public OneArgInstanceFunc(ConditionHelper.Condition field, ConditionHelper.Condition arg) {
            _arg = arg;
            _field = field;
        }
        
        public override object Get(Session session, object? userdata) {
            var field = _field.Get<TField>(session, userdata);
            var arg = _arg.Get<TArg>(session, userdata);

            return TOp.Invoke(field, arg)!;
        }
    }


    internal struct StringMatch : IOneArgFunc<string, string, int> {
        public static int Invoke(string field, string arg) {
            return Regex.IsMatch(field, arg, RegexOptions.Compiled) ? 1 : 0;
        }
    }
    
    internal struct Str : IOneArgFunc<object, string, string> {
        public static string Invoke(object field, string arg) {
            if (field is IFormattable formattable) {
                return formattable.ToString(arg, CultureInfo.InvariantCulture);
            }
            
            return field.ToString() ?? "";
        }
    }
}
