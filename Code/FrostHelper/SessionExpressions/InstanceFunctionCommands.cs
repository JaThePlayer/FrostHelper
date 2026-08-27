using FrostHelper.API;
using FrostHelper.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace FrostHelper.SessionExpressions;

internal sealed record InstanceFunctionCommand(InstanceFunctionCommands.InstanceFunctionCommandFactory Factory, CommandDescriptor Descriptor);

internal static class InstanceFunctionCommands {
    static InstanceFunctionCommands() {
        Register<object, string, string, Str>("str", [ ApiRenderPart.Default("Converts the value to a string.") ]);
        Register<string, string, int, StringIsMatch>("isMatch", [ ApiRenderPart.Default("Checks whether the string matches the given regex.") ]);
        
        RegisterSession<IEnumerable, LambdaCondition, float, EnumerableSum>("sum", [
            ApiRenderPart.Default("Calculates the sum of the results of applying the callback to every element in the collection.")
        ]);
        RegisterSession<IEnumerable, LambdaCondition, int, EnumerableAll>("all", [
            ApiRenderPart.Default("Checks whether all elements in the collection match the given predicate.")
        ]);
        RegisterSession<IEnumerable, LambdaCondition, int, EnumerableAny>("any", [
            ApiRenderPart.Default("Checks whether any element in the collection match the given predicate.")
        ]);
    }
    
    public delegate bool InstanceFunctionCommandFactory(
        ConditionHelper.Condition field, IReadOnlyList<ConditionHelper.Condition> args, [NotNullWhen(true)] out ConditionHelper.Condition? result, 
        [NotNullWhen(false)] out string? errorMessage);
    
    internal static readonly Dictionary<(Type, string), InstanceFunctionCommand> Functions = new();
    
    private static void Register(string name, Type objType, IReadOnlyList<ArgumentDescriptor> arguments, TypeDescriptor returnType, IReadOnlyList<ApiRenderPart> description, InstanceFunctionCommandFactory factory) {
        Functions[(objType, name)] = new InstanceFunctionCommand(factory, new CommandDescriptor {
            Name = name,
            Description = description,
            Arguments = arguments,
            ReturnType = returnType,
        });
    }
    
    private static void Register<TField, TArg, TResult, TOp>(string name, IReadOnlyList<ApiRenderPart> description)
        where TOp : struct, IOneArgFunc<TField, TArg, TResult> {
        Register(name, typeof(TField), [
                new ArgumentDescriptor(TOp.ArgName, TypeDescriptor.For(typeof(TArg)))
            ],
            TypeDescriptor.For(typeof(TResult)),
            description,
            OneArgInstanceFunc<TField, TArg, TResult, TOp>.TryCreate);
    }
    
    private static void RegisterSession<TField, TArg, TResult, TOp>(string name, IReadOnlyList<ApiRenderPart> description)
        where TOp : struct, IOneArgSessionFunc<TField, TArg, TResult> {
        Register(name, typeof(TField), [
                new ArgumentDescriptor(TOp.ArgName, TypeDescriptor.For(typeof(TArg)))
            ],
            TypeDescriptor.For(typeof(TResult)),
            description,
            OneArgSessionInstanceFunc<TField, TArg, TResult, TOp>.TryCreate);
    }
    
    internal static ConditionHelper.Condition Create(string functionName, ConditionHelper.Condition target, IReadOnlyList<ConditionHelper.Condition> arguments, IExpressionContext ctx) {
        if (target.ReturnType is { } knownType && GetFactory(knownType, functionName, ctx) is { } factory) {
            if (!factory.Factory(target, arguments, out var condition, out var errorMessage)) {
                NotificationHelper.Notify($"Failed to create Session Expression function: '{functionName}', called on '{knownType}':\n{errorMessage}");
                return new ConstInt(0);
            }

            condition.Descriptor = factory.Descriptor;
            return condition;
        }
        
        return new DynamicInstanceFunction(functionName, target, arguments, ctx);
    }

    internal static InstanceFunctionCommand? GetFactory(Type? type, string functionName, IExpressionContext ctx) {
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

    internal interface IInstanceFunctionCommand {
        public ConditionHelper.Condition FieldCondition { get; }
    }

    internal interface IOneArgFunc<in TField, in TArg, out TResult> {
        public static abstract TResult Invoke(TField field, TArg arg);
        
        public static abstract string ArgName { get; }
        
        public static abstract bool Emit(ConditionCompilationCtx ctx, Type targetType);
    }
    
    internal interface IOneArgSessionFunc<in TField, in TArg, out TResult> {
        public static abstract TResult Invoke(Session session, object? userdata, TField field, TArg arg);
        
        public static abstract string ArgName { get; }
        
        public static abstract bool EmitCodeUsesSessionAndUserdataOnStack { get; }
        
        public static abstract bool Emit(ConditionCompilationCtx ctx, Type targetType, ConditionHelper.Condition fieldCondition, ConditionHelper.Condition argCondition);

        public static abstract void OnCreated(ConditionHelper.Condition fieldCondition,
            ConditionHelper.Condition argCondition);
    }

    internal sealed class DynamicInstanceFunction(string functionName, ConditionHelper.Condition target, IReadOnlyList<ConditionHelper.Condition> arguments, IExpressionContext ctx) : ConditionHelper.Condition {
        public override object Get(Session session, object? userdata) {
            var obj = target.Get(session, userdata);
            if (_cache.TryGetValue(obj.GetType(), out var cached))
                return cached.Get(session, userdata);

            var factory = GetFactory(obj.GetType(), functionName, ctx);
            string? errorMessage = null;
            if (factory is null || !factory.Factory(target, arguments, out var condition, out errorMessage)) {
                NotificationHelper.Notify($"Failed to create Session Expression function: '{functionName}', called on '{obj.GetType()}':\n{errorMessage ?? "function not found"}");
                _cache[obj.GetType()] = new ConstInt(0);
                return Zero;
            }

            condition.Descriptor = factory.Descriptor;
            _cache[obj.GetType()] = condition;
            return condition.Get(session, userdata);
        }
        
        private readonly Dictionary<Type, ConditionHelper.Condition> _cache = [];
    }

    internal sealed class OneArgInstanceFunc<TField, TArg, TResult, TOp> : ConditionHelper.Condition, IInstanceFunctionCommand
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
            
            if (!TOp.Emit(ctx, targetType)) {
                il.Emit(OpCodes.Call, typeof(TOp).GetMethod(nameof(TOp.Invoke))!);
                ctx.EmitConvertTo(typeof(TResult), targetType);
            }
        }

        internal override bool UsesCurrentConditionLocalInEmit =>
            _field.UsesCurrentConditionLocalInEmit || _arg.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => typeof(TResult);

        public ConditionHelper.Condition FieldCondition => _field;
    }

    internal sealed class OneArgSessionInstanceFunc<TField, TArg, TResult, TOp> : ConditionHelper.Condition, IInstanceFunctionCommand
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
            
            TOp.OnCreated(field, arg);
        }
        
        public override object Get(Session session, object? userdata) {
            var field = _field.Get<TField>(session, userdata);
            var arg = _arg.Get<TArg>(session, userdata);

            return TOp.Invoke(session, userdata, field, arg)!;
        }
        
        internal override void Emit(ConditionCompilationCtx ctx, Type targetType) {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;

            if (TOp.EmitCodeUsesSessionAndUserdataOnStack) {
                ctx.EmitLoadSession();
                ctx.EmitLoadUserdata();
            }
            
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _field, FieldField);
            _field.Emit(ctx, typeof(TField));
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _arg, ArgField);
            _arg.Emit(ctx, typeof(TArg));
            
            il.EmitRevertCurrentCondition(tempOrigCond, ctx);

            if (!TOp.Emit(ctx, targetType, _field, _arg)) {
                il.Emit(OpCodes.Call, typeof(TOp).GetMethod(nameof(TOp.Invoke))!);
                ctx.EmitConvertTo(typeof(TResult), targetType);
            }
        }

        internal override bool UsesCurrentConditionLocalInEmit =>
            _field.UsesCurrentConditionLocalInEmit || _arg.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => typeof(TResult);
        
        public ConditionHelper.Condition FieldCondition => _field;
    }

    internal struct StringIsMatch : IOneArgFunc<string, string, int> {
        public static int Invoke(string field, string arg) {
            return Regex.IsMatch(field, arg, RegexOptions.Compiled) ? 1 : 0;
        }

        public static string ArgName => "regex";

        public static bool Emit(ConditionCompilationCtx ctx, Type targetType) => false;
    }
    
    internal struct Str : IOneArgFunc<object, string, string> {
        public static string Invoke(object field, string arg) {
            if (field is IFormattable formattable) {
                try {
                    return formattable.ToString(arg, CultureInfo.InvariantCulture);
                } catch (FormatException) {
                    NotificationHelper.Notify($"Invalid format string as 'str()' argument: {arg}");
                }
            }
            
            return field.ToString() ?? "";
        }

        public static string ArgName => "format";

        public static bool Emit(ConditionCompilationCtx ctx, Type targetType) => false;
    }

    private static class EnumerableEmitUtils {
        /// <summary>
        /// Exprects [ field(IEnumerable), arg(LambdaCondition) ] on the stack
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="fieldCondition"></param>
        /// <param name="argCondition">Should be a lambda definition</param>
        /// <param name="emitPostLambdaCallCode">(endLoopLabel)</param>
        public static void EmitEnumerateThroughFieldAndCallArg<TLambdaReturnType>(ConditionCompilationCtx ctx, ConditionHelper.Condition fieldCondition,
            ConditionHelper.Condition argCondition, Action<Label> emitPostLambdaCallCode) {
            
            var il = ctx.Il;
            LocalBuilder? tempCurrentCond = null;
            if (argCondition is not LambdaDefinitionCondition lambdaDefinitionCondition) {
                throw new Exception("Expected argument to IEnumerable fold to be a lambda.");
            }
            var lambda = lambdaDefinitionCondition.Instance;
            var lambdaLocal = il.DeclareLocal(typeof(LambdaCondition));
            il.Emit(OpCodes.Stloc, lambdaLocal);

            // field(IEnumerable) is now top of stack
            var enumerableType = fieldCondition.ReturnType ?? typeof(IEnumerable);
            var getEnumerator = enumerableType.GetMethod(nameof(IEnumerable.GetEnumerator));
            if (getEnumerator is null)
                throw new Exception("Enumerable type doesn't have GetEnumerator???");
            
            var enumeratorLocal = il.DeclareLocal(getEnumerator.ReturnType);
            il.Emit(OpCodes.Callvirt, getEnumerator);
            il.Emit(OpCodes.Stloc, enumeratorLocal);
            
            var nextElementLabel = il.DefineLabel();
            var endLoopLabel = il.DefineLabel();
            
            il.MarkLabel(nextElementLabel);
            
            il.EmitLdlocOrLdloca(enumeratorLocal);
            il.Emit(OpCodes.Callvirt, getEnumerator.ReturnType.GetMethod(nameof(IEnumerator.MoveNext))
                                          ?? typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!);
            il.Emit(OpCodes.Brfalse, endLoopLabel);
            
            il.Emit(OpCodes.Ldloc, lambdaLocal);
            il.Emit(OpCodes.Ldc_I4_0);
            il.EmitLdlocOrLdloca(enumeratorLocal);
            var currentProp = getEnumerator.ReturnType.GetProperty(nameof(IEnumerator.Current))!.GetMethod!;
            lambdaDefinitionCondition.ArgumentTypes[0] = currentProp.ReturnType;
            il.Emit(OpCodes.Callvirt, currentProp);
            ctx.EmitConvertTo(currentProp.ReturnType, typeof(object));
            lambda.EmitSetArgument(ctx);
            
            ctx.EmitSwapOutCurrentCondition(ref tempCurrentCond, lambda, () => {
                il.Emit(OpCodes.Ldloc, lambdaLocal);
            });
            lambda.Emit(ctx, typeof(TLambdaReturnType));

            emitPostLambdaCallCode(endLoopLabel);
            il.Emit(OpCodes.Br, nextElementLabel);
            
            il.MarkLabel(endLoopLabel);
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

        public static string ArgName => "callback";

        public static bool EmitCodeUsesSessionAndUserdataOnStack => false;

        public static bool Emit(ConditionCompilationCtx ctx, Type targetType, ConditionHelper.Condition fieldCondition, ConditionHelper.Condition argCondition) {
            var il = ctx.Il;
            var sumLocal = il.DeclareLocal(typeof(float));
            il.Emit(OpCodes.Ldc_R4, 0f);
            il.Emit(OpCodes.Stloc, sumLocal);
            
            EnumerableEmitUtils.EmitEnumerateThroughFieldAndCallArg<float>(ctx, fieldCondition, argCondition,
                emitPostLambdaCallCode: (endLoopLabel) => {
                    il.Emit(OpCodes.Ldloc, sumLocal);
                    il.Emit(OpCodes.Add);
                    il.Emit(OpCodes.Stloc, sumLocal);
                });
            
            il.Emit(OpCodes.Ldloc, sumLocal);
            ctx.EmitConvertTo(typeof(float), targetType);
            return true;
        }

        public static void OnCreated(ConditionHelper.Condition fieldCondition, ConditionHelper.Condition argCondition) {
        }
    }
    
    internal struct EnumerableAll : IOneArgSessionFunc<IEnumerable, LambdaCondition, int> {
        public static int Invoke(Session session, object? userdata, IEnumerable field, LambdaCondition callback) {
            foreach (var obj in field) {
                callback.SetArgument(0, obj);
                if (!callback.Check(session, userdata))
                    return 0;
            }

            return 1;
        }

        public static string ArgName => "predicate";

        public static bool EmitCodeUsesSessionAndUserdataOnStack => false;

        public static bool Emit(ConditionCompilationCtx ctx, Type targetType, ConditionHelper.Condition fieldCondition, ConditionHelper.Condition argCondition) {
            var il = ctx.Il;
            var retLocal = il.DeclareLocal(typeof(bool));
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Stloc, retLocal);
            
            EnumerableEmitUtils.EmitEnumerateThroughFieldAndCallArg<float>(ctx, fieldCondition, argCondition,
                emitPostLambdaCallCode: (endLoopLabel) => {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, retLocal);
                    il.Emit(OpCodes.Brfalse, endLoopLabel);
                });
            
            il.Emit(OpCodes.Ldloc, retLocal);
            ctx.EmitConvertTo(typeof(bool), targetType);
            return true;
        }

        public static void OnCreated(ConditionHelper.Condition fieldCondition, ConditionHelper.Condition argCondition) {
        }
    }
    
    internal struct EnumerableAny : IOneArgSessionFunc<IEnumerable, LambdaCondition, int> {
        public static int Invoke(Session session, object? userdata, IEnumerable field, LambdaCondition callback) {
            foreach (var obj in field) {
                callback.SetArgument(0, obj);
                if (callback.Check(session, userdata))
                    return 1;
            }

            return 0;
        }

        public static string ArgName => "predicate";

        public static bool EmitCodeUsesSessionAndUserdataOnStack => false;

        public static bool Emit(ConditionCompilationCtx ctx, Type targetType, ConditionHelper.Condition fieldCondition, ConditionHelper.Condition argCondition) {
            var il = ctx.Il;
            var retLocal = il.DeclareLocal(typeof(bool));
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, retLocal);
            
            EnumerableEmitUtils.EmitEnumerateThroughFieldAndCallArg<float>(ctx, fieldCondition, argCondition,
                emitPostLambdaCallCode: (endLoopLabel) => {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Stloc, retLocal);
                    il.Emit(OpCodes.Brtrue, endLoopLabel);
                });
            
            il.Emit(OpCodes.Ldloc, retLocal);
            ctx.EmitConvertTo(typeof(bool), targetType);
            return true;
        }

        public static void OnCreated(ConditionHelper.Condition fieldCondition, ConditionHelper.Condition argCondition) {
        }
    }
}
