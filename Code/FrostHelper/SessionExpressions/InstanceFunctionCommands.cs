using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal static class InstanceFunctionCommands {
    public delegate bool InstanceFunctionCommandFactory(
        ConditionHelper.Condition field, IReadOnlyList<ConditionHelper.Condition> args, [NotNullWhen(true)] out ConditionHelper.Condition? result, 
        [NotNullWhen(false)] out string? errorMessage);
    
    internal static readonly Dictionary<(Type, string), InstanceFunctionCommandFactory> Functions = new() {
        [(typeof(object), "str")] = OneArgInstanceFunc<object, string, string, Str>.TryCreate,
        
        [(typeof(string), "match")] = OneArgInstanceFunc<string, string, int, StringMatch>.TryCreate,
        
        [(typeof(IEnumerable), "sum")] = OneArgSessionInstanceFunc<IEnumerable, LambdaCondition, float, EnumerableSum>.TryCreate,
    };
    
    internal static ConditionHelper.Condition Create(string functionName, ConditionHelper.Condition target, IReadOnlyList<ConditionHelper.Condition> arguments, IExpressionContext ctx) {
        if (target.ReturnType is { } knownType && GetFactory(knownType, functionName, ctx) is { } factory) {
            if (!factory(target, arguments, out var condition, out var errorMessage)) {
                NotificationHelper.Notify($"Failed to create Session Expression function: '{functionName}', called on '{knownType}':\n{errorMessage}");
                return new ConstInt(0);
            }

            return condition;
        }
        
        return new DynamicInstanceFunction(functionName, target, arguments, ctx);
    }

    internal static InstanceFunctionCommandFactory? GetFactory(Type? type, string functionName, IExpressionContext ctx) {
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
    
    internal interface IOneArgSessionFunc<in TField, in TArg, out TResult> {
        public static abstract TResult Invoke(Session session, object? userdata, TField field, TArg arg);
    }

    internal sealed class DynamicInstanceFunction(string functionName, ConditionHelper.Condition target, IReadOnlyList<ConditionHelper.Condition> arguments, IExpressionContext ctx) : ConditionHelper.Condition {
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

        private static readonly FieldInfo FieldField =
            typeof(OneArgInstanceFunc<TField, TArg, TResult, TOp>).GetField(nameof(_field),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        private static readonly FieldInfo ArgField =
            typeof(OneArgInstanceFunc<TField, TArg, TResult, TOp>).GetField(nameof(_arg),
                BindingFlags.Instance | BindingFlags.NonPublic)!;

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

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;
            
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _field, FieldField);
            _field.Emit(ctx, typeof(TField));
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _arg, ArgField);
            _arg.Emit(ctx, typeof(TArg));
            
            il.EmitRevertCurrentCondition(tempOrigCond, ctx);
            
            il.Emit(OpCodes.Call, typeof(TOp).GetMethod(nameof(TOp.Invoke))!);
            ctx.EmitConvertTo(typeof(TResult), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit =>
            _field.UsesCurrentConditionLocalInEmit || _arg.UsesCurrentConditionLocalInEmit;
    }

    internal sealed class OneArgSessionInstanceFunc<TField, TArg, TResult, TOp> : ConditionHelper.Condition
        where TOp : IOneArgSessionFunc<TField, TArg, TResult> {

        private readonly ConditionHelper.Condition _field;
        private readonly ConditionHelper.Condition _arg;
        private static readonly FieldInfo FieldField =
            typeof(OneArgSessionInstanceFunc<TField, TArg, TResult, TOp>).GetField(nameof(_field),
                BindingFlags.Instance | BindingFlags.NonPublic)!;
        
        private static readonly FieldInfo ArgField =
            typeof(OneArgSessionInstanceFunc<TField, TArg, TResult, TOp>).GetField(nameof(_arg),
                BindingFlags.Instance | BindingFlags.NonPublic)!;

        public static bool TryCreate(ConditionHelper.Condition field, IReadOnlyList<ConditionHelper.Condition> args,
            [NotNullWhen(true)] out ConditionHelper.Condition? result,
            [NotNullWhen(false)] out string? errorMessage) {
            result = null;
            errorMessage = null;

            if (args is not [{ } onlyArg]) {
                return FunctionCommands.FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out result, out errorMessage);
            }
                
            result = new OneArgSessionInstanceFunc<TField, TArg, TResult, TOp>(field, onlyArg);
            return true;
        }

        public OneArgSessionInstanceFunc(ConditionHelper.Condition field, ConditionHelper.Condition arg) {
            _arg = arg;
            _field = field;
        }
        
        public override object Get(Session session, object? userdata) {
            var field = _field.Get<TField>(session, userdata);
            var arg = _arg.Get<TArg>(session, userdata);

            return TOp.Invoke(session, userdata, field, arg)!;
        }
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;
            
            ctx.EmitLoadSession();
            ctx.EmitLoadUserdata();
            
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _field, FieldField);
            _field.Emit(ctx, typeof(TField));
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _arg, ArgField);
            _arg.Emit(ctx, typeof(TArg));
            
            il.EmitRevertCurrentCondition(tempOrigCond, ctx);
            
            il.Emit(OpCodes.Call, typeof(TOp).GetMethod(nameof(TOp.Invoke))!);
            ctx.EmitConvertTo(typeof(TResult), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit =>
            _field.UsesCurrentConditionLocalInEmit || _arg.UsesCurrentConditionLocalInEmit;
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
    
    internal struct EnumerableSum : IOneArgSessionFunc<IEnumerable, LambdaCondition, float> {
        public static float Invoke(Session session, object? userdata, IEnumerable field, LambdaCondition callback) {
            float sum = 0;
            foreach (var obj in field) {
                callback.SetArgument(0, obj);
                sum += callback.GetFloat(session, userdata);
            }

            return sum;
        }
    }
}
